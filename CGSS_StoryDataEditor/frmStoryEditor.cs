using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace CGSS_StoryDataEditor
{
    public partial class frmStoryEditor : Form
    {
        List<Story.Data.CommandStruct> storyCommand;
        Story.Data.Config _config;
        System.Text.RegularExpressions.Regex _logParentheses;
        System.Text.RegularExpressions.Regex _headOnly;
        System.Text.RegularExpressions.Regex _tailOnly;

        int saveRunning = 0;
        public frmStoryEditor()
        {
            InitializeComponent();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            File.WriteAllBytes("./story.bytes.saved" + saveRunning.ToString(), takumiSerialize(takumiSerializeLine(storyCommand)));
            saveRunning++;
        }
        private void button1_Click(object sender, EventArgs e)
        {

            _config = Story.Data.Config.Create();
            var bytes = File.ReadAllBytes("./story.bytes");

            Story.Data.Parser storyParser = new Story.Data.Parser();
            storyParser.Init();

            storyCommand = storyParser.ConvertBinaryToCommandList(bytes);
            for (int i = 0; i < storyCommand.Count; i++)
            {
                listBox1.Items.Add(i.ToString() + " - " + storyCommand[i].Name.ToString());
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                listBox2.Items.Clear();
                for (int j = 0; j < storyCommand[listBox1.SelectedIndex].Args.Count; j++)
                {
                    listBox2.Items.Add(j.ToString() + " - " + storyCommand[listBox1.SelectedIndex].Args[j].ToString());
                }
                //textBox1.Text = storyCommand[listBox1.SelectedIndex].Args.ToString();
            }
            catch (Exception ex)
            {

            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = storyCommand[listBox1.SelectedIndex].Args[listBox2.SelectedIndex].ToString();
            }
            catch (Exception ex)
            {

            }
         }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                storyCommand[listBox1.SelectedIndex].Args[listBox2.SelectedIndex] = textBox1.Text;
                listBox1_SelectedIndexChanged(null, null);
                _config = Story.Data.Config.Create();
            }
            catch (Exception ex)
            {

            }
        }

        private List<byte[]> takumiSerializeLine(List<Story.Data.CommandStruct> cmdStruct)
        {
            List<byte[]> byteLine = new List<byte[]>();
            

            for (int i = 0; i < cmdStruct.Count; i++)
            {
                byte[] byteTemp = new byte[0];
                byte[] tmpOne = ConvertByteCommand(cmdStruct[i].Name.ToString());
                byteTemp = ByteListAppend(ref byteTemp, ref tmpOne);
                for (int j = 0; j < cmdStruct[i].Args.Count; j++)
                {
                    

                    byte[] tmpTwo = ConvertByteArgs(cmdStruct[i].Args[j].ToString());

                    int length = tmpTwo.Length;
                    byte[] tmpLength = BitConverter.GetBytes(length);
                    Array.Reverse(tmpLength);
                    byteTemp = ByteListAppend(ref byteTemp, ref tmpLength);
                    byteTemp = ByteListAppend(ref byteTemp, ref tmpTwo);
                }
                byteLine.Add(byteTemp);
            }

            return byteLine;

        }

        private byte[] takumiSerialize(List<byte[]> byteLine)
        {
            byte[] padFour = { 0x00, 0x00, 0x00, 0x00 };
            byte[] byteTemp = new byte[0];

            for (int i = 0; i < byteLine.Count; i++)
            {
                byte[] padding = new byte[2];
                Array.Copy(byteLine[i], 0, padding,0, 2);
                byteTemp = ByteListAppend(ref byteTemp, ref padding);
                //int length = byteLine[i].Length-2;
                //byte[] tmpLength = BitConverter.GetBytes(length);
                //Array.Reverse(tmpLength);
                //byteTemp = ByteListAppend(ref byteTemp, ref tmpLength);
                byte[] tmpByte = new byte[byteLine[i].Length - 2];
                Array.Copy(byteLine[i], 2, tmpByte, 0, byteLine[i].Length - 2);
                byteTemp = ByteListAppend(ref byteTemp, ref tmpByte);
                byteTemp = ByteListAppend(ref byteTemp, ref padFour);

            }
            return byteTemp;
        }

        private List<byte[]> Serialize(ref List<string> fileData)
        {
            List<byte[]> list = new List<byte[]>();
            int count = fileData.Count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    byte[] item = this.SerializeLine(fileData[i]);
                    list.Add(item);
                }
                catch (Exception ex)
                {
                    string message = string.Format("StoryData Convert Error {0} : {1}", i + 1, ex);
                    throw new Exception(message, ex);
                }
            }
            return list;
        }

        private byte[] SerializeLine(string commandLine)
        {
            commandLine = this._logParentheses.Replace(commandLine, "log $1 $2 \"（$3）\"");
            List<string> list = this.SplitString(ref commandLine);
            List<List<string>> list2 = new List<List<string>>();
            int count = list.Count;
            int num = 0;
            for (int i = 0; i < count; i++)
            {
                if (list[i] == "<")
                {
                    List<string> range = list.GetRange(num, i - num);
                    num = i + 1;
                    list2.Add(range);
                }
            }
            if (num < count)
            {
                List<string> range2 = list.GetRange(num, count - num);
                list2.Add(range2);
            }
            byte[] result = new byte[0];
            byte[] bytes = BitConverter.GetBytes(0);
            int count2 = list2.Count;
            for (int j = 0; j < count2; j++)
            {
                List<string> list3 = list2[j];
                byte[] array = this.ConvertByteCommand(list3[0]);
                result = this.ByteListAppend(ref result, ref array);
                count = list3.Count;
                string text = list3[0];
                int commandID = this._config.GetCommandID(ref text);
                int num2 = count - 1;
                if (commandID == -1)
                {
                    if (num2 > 0 || text != string.Empty)
                    {
                        throw new Exception("不正なコマンドです : " + text);
                    }
                }
                else
                {
                    int commandMinArgCount = this._config.GetCommandMinArgCount(commandID);
                    int commandMaxArgCount = this._config.GetCommandMaxArgCount(commandID);
                    if (num2 < commandMinArgCount || num2 > commandMaxArgCount)
                    {
                        throw new ArgumentOutOfRangeException("引数の数が合いません");
                    }
                    for (int k = 1; k < count; k++)
                    {
                        byte[] array2 = this.ConvertByteArgs(list3[k]);
                        int value = array2.Length;
                        byte[] bytes2 = BitConverter.GetBytes(value);
                        Array.Reverse(bytes2);
                        result = this.ByteListAppend(ref result, ref bytes2);
                        result = this.ByteListAppend(ref result, ref array2);
                    }
                    result = this.ByteListAppend(ref result, ref bytes);
                }
            }
            if (list.IndexOf("print") >= 0 || list.IndexOf("double") >= 0)
            {
                byte[] array3 = this.ConvertByteCommand("touch");
                result = this.ByteListAppend(ref result, ref array3);
                result = this.ByteListAppend(ref result, ref bytes);
            }
            return result;
        }

        // Story.Data.Parser
        private List<string> SplitString(ref string commandLine)
        {
            List<string> list = new List<string>();
            List<bool> list2 = new List<bool>();
            int length = commandLine.Length;
            int num = 0;
            for (int i = 0; i < length; i++)
            {
                string a = commandLine.Substring(i, 1);
                int index = list2.Count - 1;
                if (a == " " && list2.Count == 0)
                {
                    string item = commandLine.Substring(num, i - num);
                    list.Add(item);
                    num = i + 1;
                }
                else if (a == "\"")
                {
                    if (list2.Count == 0)
                    {
                        list2.Add(true);
                    }
                    else if (!list2[index])
                    {
                        list2.Add(true);
                    }
                    else
                    {
                        list2.RemoveAt(index);
                    }
                }
                else if (a == "<")
                {
                    string item2 = commandLine.Substring(num, i - num);
                    list.Add(item2);
                    num = i + 1;
                    list.Add("<");
                    list2.Add(false);
                }
                else if (a == ">")
                {
                    if (!list2[index])
                    {
                        list2.RemoveAt(index);
                    }
                    string text = commandLine.Substring(num, i - num);
                    List<string> list3 = this.SplitString(ref text);
                    int count = list3.Count;
                    for (int j = 0; j < count; j++)
                    {
                        list.Add(list3[j]);
                    }
                    list.Add("<");
                    int count2 = list.Count;
                    int num2 = 0;
                    while (num2 < count2 && list[num2] != "<")
                    {
                        string item3 = list[num2];
                        list.Add(item3);
                        num2++;
                    }
                    list.RemoveAt(list.Count - 1);
                    num = i + 1;
                }
            }
            if (num < length)
            {
                string item4 = commandLine.Substring(num, length - num);
                list.Add(item4);
            }
            int count3 = list.Count;
            for (int k = 0; k < count3; k++)
            {
                if (list[k] == "print" || list[k] == "double")
                {
                    string text2 = list[k + 2];
                    if (text2.Length == 0 || (text2.Length == 1 && text2 == "\""))
                    {
                        list.RemoveRange(k, 3);
                        if (list.Count > k && list[k] == "<")
                        {
                            list.RemoveAt(k);
                        }
                        count3 = list.Count;
                    }
                }
                else
                {
                    string text3 = this._headOnly.Replace(list[k], "$1");
                    text3 = this._tailOnly.Replace(text3, "$1");
                    list[k] = text3;
                }
            }
            list.Remove(string.Empty);
            return list;
        }
        // Story.Data.Parser
        private byte[] ByteListAppend(ref byte[] baseList, ref byte[] addList)
        {
            int num = baseList.Length;
            int num2 = addList.Length;
            byte[] array = new byte[num + num2];
            Array.Copy(baseList, array, num);
            Array.Copy(addList, 0, array, num, num2);
            return array;
        }

        // Story.Data.Parser
        private byte[] ConvertByteCommand(string command)
        {
            int commandID = this.GetCommandID(ref command);
            byte[] bytes = BitConverter.GetBytes(commandID);
            Array.Resize<byte>(ref bytes, 2);
            Array.Reverse(bytes);
            return bytes;
        }

        // Story.Data.Parser
        private byte[] ConvertByteArgs(string args)
        {
            Encoding uTF = Encoding.UTF8;
            byte[] bytes = uTF.GetBytes(args);
            string s = Convert.ToBase64String(bytes);
            bytes = uTF.GetBytes(s);
            this.BitInverse(ref bytes);
            return bytes;
        }

        // Story.Data.Parser
        private void BitInverse(ref byte[] byteList)
        {
            int num = byteList.Length;
            for (int i = 0; i < num; i++)
            {
                if (i % 3 == 0)
                {
                    byteList[i] = BitConverter.GetBytes((int)(~(int)byteList[i]))[0];
                }
            }
        }

        // Story.Data.Parser
        private int GetCommandID(ref string command)
        {
            return this._config.GetCommandID(ref command);
        }

        private void frmStoryEditor_Load(object sender, EventArgs e)
        {

        }

       






    }
}
