using System.Diagnostics.Metrics;

namespace jpeg
{
    internal class Node : IComparable<Node>
    {
        public int Value { get; set; }
        public int Amount { get; set; }
        public string Code { get; set; }
        public Node? Left { get; set; }
        public Node? Right { get; set; }

        public Node(int value, int amount)
        {
            Value = value;
            Amount = amount;
            Left = null;
            Right = null;
        }
        public Node(KeyValuePair<int, int> kvp)
        {
            Value = kvp.Key;
            Amount = kvp.Value;
            Left = null;
            Right = null;
        }
        public Node(Node left, Node right)
        {
            Left = left;
            Right = right;
            Amount = Left.Amount + Right.Amount;
        }
        public void WriteCodes(string code = "")
        {
            if (Left == null && Right == null)
            {
                Code = code;
                Console.WriteLine($"{Value} - {Code}\t{Amount}");
            }
            else
            {
                if (Left != null)
                    Left.WriteCodes(code + "1");
                if (Right != null)
                    Right.WriteCodes(code + "0");
            }
        }
        public void ReadCodes(byte[] counts)
        {
            if (Left == null && Right == null)
                counts[Code.Length - 1]++;
            else
            {
                if (Left != null)
                    Left.ReadCodes(counts);
                if (Right != null)
                    Right.ReadCodes(counts);
            }
        }
        public List<Node> GetNodeList()
        {
            List<Node> list = new List<Node>();

            this.GetNodeListRecursion(list);

            return list;
        }
        void GetNodeListRecursion(List<Node> list)
        {
            if (Left == null && Right == null)
                list.Add(this);
            else
            {
                if (Left != null)
                    Left.GetNodeListRecursion(list);
                if (Right != null)
                    Right.GetNodeListRecursion(list);
            }
        }
        public int CompareTo(Node? that)
        {
            return that != null ? this.Amount.CompareTo(that.Amount) : Math.Sign(this.Amount); ;
        }
    }
}
