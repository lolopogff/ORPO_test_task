using System.IO;
using System.Text;

namespace ForbiddenOrgChecker.Helpers
{
    public class DualTextWriter : TextWriter
    {
        private readonly TextWriter console;
        private readonly TextWriter file;

        public DualTextWriter(TextWriter console, TextWriter file)
        {
            this.console = console;
            this.file = file;
        }

        public override Encoding Encoding => console.Encoding;

        public override void Write(char value)
        {
            console.Write(value);
            file.Write(value);
        }

        public override void Write(string? value)
        {
            console.Write(value);
            file.Write(value);
        }

        public override void WriteLine(string? value)
        {
            console.WriteLine(value);
            file.WriteLine(value);
        }

        public override void Flush()
        {
            console.Flush();
            file.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                file?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}