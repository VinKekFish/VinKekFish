using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator
{
    public class BaseCSharpCodeGenerator
    {
        public readonly StringBuilder sb          = new StringBuilder(1024);

        public          string currentTabs = "";
        public readonly string FileName;
        public BaseCSharpCodeGenerator(string FileName, string currentTabs = "", params string[] @using)
        {
            this.FileName    = FileName;
            this.currentTabs = currentTabs;

            if (@using.Length > 0)
            {
                foreach (var d in @using)
                    Add("using " + d + ";");
            }
        }

        public void EndGeneration()
        {
            while (currentTabs.Length > 0)
                endBlock();
        }

        public void Save()
        {
            File.WriteAllText(FileName, sb.ToString());
        }

        public void addForHeader(string init, string condition, string post)
        {
            Add("for (" + init + "; " + condition + "; " + post + ")");
            addBlock();
        }

        public void addFuncHeader(string modificator, string type, string Name, string @params = "")
        {
            Add(modificator + " " + type + " " + Name + "(" + @params + ")");
            addBlock();
        }

        public void addClassHeader(string modificator, string Name, string Parents = "")
        {
            if (!string.IsNullOrEmpty(Parents))
                Parents = ": " + Parents;

            Add(modificator + " class " + Name + " " + Parents);
            addBlock();
        }

        public void addBlock()
        {
            Add("{");
            currentTabs += "\t";
        }

        public void endBlock()
        {
            currentTabs = currentTabs.Remove(currentTabs.Length - 1);
            Add("}");
            Add("");
        }

        public void Add(string line)
        {
            sb.AppendLine(currentTabs + line);
        }
    }
}
