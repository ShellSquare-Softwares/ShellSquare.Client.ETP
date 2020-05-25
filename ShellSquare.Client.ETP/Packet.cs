namespace ShellSquare.Client.ETP
{
    internal class Packet
    {
        public string Index { get; set; }
        public string Data { get; set; }

        public override string ToString()
        {
            if (Data == null)
            {
                Data = "";
            }

            return $"{Index},{Data}";
        }
    }
}