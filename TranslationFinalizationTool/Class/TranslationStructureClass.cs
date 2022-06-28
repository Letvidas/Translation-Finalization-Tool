using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslationFinalizationTool.Class
{
    public class TranslationStructureClass
    {
        public List<string> StartLine = new();
        public List<string> Source = new();
        public List<string> Target = new();
        public List<string> Note1 = new();
        public List<string> Note2 = new();
        public List<string> EndLine = new();
        public List<string> FileStart = new();
        public List<string> FileEnd = new();
        public List<string> SearchNote2Parameters = new();
        public Boolean Full;
    }
}
