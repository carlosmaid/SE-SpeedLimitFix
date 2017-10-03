using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedLimitFix
{
#if DEBUG
    public static class Log
    {
        static Stream logStream;
        static TextWriter logWritter;
        static Log()
        {
            logStream = new FileStream("D:\\slf.log", FileMode.Append, FileAccess.Write, FileShare.Read);
            logWritter = new StreamWriter(logStream);
            (logWritter as StreamWriter).AutoFlush = true;
        }

        const string NewFrameSeparator = "--------------------------------------";

        public static void Write(object anObject)
        {
            if (anObject == null)
            {
                anObject = "<null>";
            }
            string str = anObject.ToString();

            WriteInternal(str);
        }

        public static void WriteFormat(string format, params object[] args)
        {
            WriteInternal(string.Format(format, args));
        }

        public static void WriteLine()
        {
            WriteInternal("");
        }

        private static void WriteInternal(string str)
        {
            logWritter.Write(str + "\n");
        }
    }
#endif
}
