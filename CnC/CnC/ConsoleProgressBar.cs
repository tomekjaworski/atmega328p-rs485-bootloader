using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CnC
{

    public class ConsoleProgressBar
    {
        private int cx, cy;
        private int width;
        private string content;
        private double min, max, progress;

        public double Progress
        {
            get { return this.progress; }
            set {
                this.progress = value;
                UpdateContent();
                Show();
            }
        }


        public ConsoleProgressBar(double min, double max, double start_value = 0, int width = 30)
        {
            this.progress = start_value;
            this.cx = Console.CursorLeft;
            this.cy = Console.CursorTop;
            this.width = width;
            this.min = min;
            this.max = max;

            this.UpdateContent();

            Show();
        }

        private void UpdateContent()
        {
            this.content = "[";

            double p = (progress - min) / (max - min);
            int pw = (int)Math.Round((double)width * p);

            for (int i = 0; i < pw; i++) this.content += '#';
            for (int i = 0; i < width - pw; i++) this.content += '.';

            this.content += "] ";
            this.content += (p * 100.0).ToString("N2") + "% ";
        }

        void Show()
        {
            Console.SetCursorPosition(cx, cy);
            Console.Write(this.content);
        }
    }
}
