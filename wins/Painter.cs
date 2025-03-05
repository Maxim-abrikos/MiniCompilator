using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Compiler
{
    internal static class Painter
    {
        //public static List<Color> ColorsOfTheText = new List<Color>();

        public static readonly Dictionary<string, Color> ColorBook = new Dictionary<string, Color>() {
            { "Вверх", Colors.Blue },
            { "Вниз", Colors.Blue },
            { "Вправо", Colors.Green },
            { "Влево", Colors.Green }
            };
        
    }
}
