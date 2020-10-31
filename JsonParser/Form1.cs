using FreeMote.PsBuild;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;


/*
 * TODO: 스레딩
 */

namespace JsonParser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // 추출 버튼
        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFile = new OpenFileDialog())
            {
                openFile.Title = "텍스트를 추출할 SCN 파일을 선택해주세요. (다중 선택 가능)";
                openFile.DefaultExt = "scn";
                openFile.Filter = "SCN 파일 (*.scn)|*.scn;";
                openFile.Multiselect = true;

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    progressBar1.Value = 0;
                    progressBar1.Maximum = openFile.FileNames.Length;
                    label2.Text = $"0/{progressBar1.Maximum}";

                    foreach (var file in openFile.FileNames)
                    {
                        PsbDecompiler.DecompileToFile(file);

                        progressBar1.PerformStep();
                        label2.Text = $"{progressBar1.Value}/{progressBar1.Maximum}";
                    }

                    progressBar1.Value = 0;

                    foreach (var file in openFile.FileNames)
                    {
                        string json = file.Substring(0, file.Length - 3) + "json";
                        Parse(json);

                        progressBar1.PerformStep();
                        label2.Text = $"{progressBar1.Value}/{progressBar1.Maximum}";
                    }
                }
            }
        }

        private void Parse(string filename)
        {
            string json = File.ReadAllText(filename);
            JObject jo = JObject.Parse(json);

            var scenes = jo.SelectToken("scenes");
            string extractedTexts = string.Empty;

            // 선택지 텍스트 추출
            foreach (var scene in scenes)
            {
                var selects = scene.SelectToken("selects");
                if (selects != null)
                {
                    foreach (var select in selects)
                    {
                        var text = select.SelectToken("text");
                        extractedTexts +=
                            $"□{text}\r\n" +
                            $"■{text}\r\n\r\n";
                    }
                }
            }

            // 게임 텍스트 추출
            foreach (var scene in scenes)
            {
                var texts = scene.SelectToken("texts");
                if (texts != null)
                {
                    foreach (var text in texts)
                    {
                        string name = text[0].ToString(),
                               displayName = text[1].ToString(),
                               line = text[2].ToString();

                        // 문자열 속 \n을 제거
                        line = line.Replace("\n", "");

                        extractedTexts += checkBox1.Checked ?
                            $"//{name}//{displayName}\r\n" +
                            // $"□{line}\r\n" +
                            $"■{line}\r\n\r\n"
                            :
                            $"//{name}//{displayName}\r\n" +
                            $"□{line}\r\n" +
                            $"■{line}\r\n\r\n";
                    }
                }
            }
            File.WriteAllText(filename + ".txt", extractedTexts);
            Console.WriteLine("추출 완료");
        }

        // 적용 버튼
        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFile = new OpenFileDialog())
            {
                openFile.Title = "적용할 SCN 파일을 선택해주세요. (다중 선택 가능)";
                openFile.DefaultExt = "scn";
                openFile.Filter = "SCN 파일 (*.scn)|*.scn;";
                openFile.Multiselect = true;

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    progressBar1.Value = 0;
                    progressBar1.Maximum = openFile.FileNames.Length;
                    label2.Text = $"0/{progressBar1.Maximum}";

                    foreach (var file in openFile.FileNames)
                    {
                        string json = file.Substring(0, file.Length - 3) + "json";
                        Apply(json);

                        progressBar1.PerformStep();
                        label2.Text = $"{progressBar1.Value}/{progressBar1.Maximum}";
                    }

                    progressBar1.Value = 0;

                    foreach (var file in openFile.FileNames)
                    {
                        string json = file.Substring(0, file.Length - 3) + "json";

                        PsbCompiler.CompileToFile(json, json, null, null, null, null);
                        var purescn = file.Substring(0, file.Length - 3) + "pure.scn";

                        File.Delete(file);
                        File.Move(purescn, file);

                        progressBar1.PerformStep();
                        label2.Text = $"{progressBar1.Value}/{progressBar1.Maximum}";
                    }
                }
            }
        }

        private void Apply(string fileName)
        {
            JObject jo = JObject.Parse(File.ReadAllText(fileName));
            string[] translatedTexts = File.ReadAllLines(fileName + ".txt", Encoding.UTF8);
            int index = 0, indexSelect = 0, pull = 0;
            for (int i = 0; i < translatedTexts.Length; i++)
            {
                if (translatedTexts[i].StartsWith("//"))
                {
                    pull = i;
                    break;
                }
            }

            for (int i = 0; i < jo["scenes"].Count(); i++)
            {
                try
                {
                    for (int j = 0; j < jo["scenes"][i]["selects"].Count(); j++)
                    {
                        jo["scenes"][i]["selects"][j]["text"] = translatedTexts[3 * indexSelect + 1].Substring(1);

                        indexSelect++;
                    }
                }
                catch
                {
                    // 아무것도 안 함
                }

                try
                {
                    for (int j = 0; j < jo["scenes"][i]["texts"].Count(); j++)
                    {
                        string[] names = translatedTexts[(checkBox1.Checked ? 3 : 4) * index + pull].Split(new string[] { "//" }, StringSplitOptions.None);
                        if (jo["scenes"][i]["texts"][j][0].Type != JTokenType.Null)
                            jo["scenes"][i]["texts"][j][0] = names[1];

                        if (jo["scenes"][i]["texts"][j][1].Type != JTokenType.Null)
                            jo["scenes"][i]["texts"][j][1] = names[2];

                        jo["scenes"][i]["texts"][j][2] = translatedTexts[(checkBox1.Checked ? 3 : 4) * index + pull + 1].Substring(1);

                        index++;
                    }
                }
                catch
                {
                    // 아무것도 안 함
                }
            }
            string json = JsonConvert.SerializeObject(jo, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }
    }
}