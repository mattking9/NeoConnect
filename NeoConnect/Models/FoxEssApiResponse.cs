namespace NeoConnect
{
    public class FoxEssApiResponse
    {
        public class ResultType
        {
            public class DataType
            {
                public string Unit { get; set; }
                public string Name { get; set; }
                public string Variable { get; set; }
                public decimal Value { get; set; }
            }

            public List<DataType> Datas { get; set; }
            public string Time { get; set; }
            public string DeviceSN { get; set; }
        }

        public int Errno { get; set; }
        public string Msg { get; set; }
        public List<ResultType> Result { get; set; }
    }
}