using System.Threading;

namespace ShellSquare.Client.ETP
{
    class EtpHelper
    {
        private static int MessageId = 1;

        public static int NextMessageId
        {
            get
            {
                return Interlocked.Increment(ref MessageId);
            }
        }
    }
}
