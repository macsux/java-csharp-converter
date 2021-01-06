namespace Converter
{
    record ClassMap
    {
        public string FromClass { get; }
        public string ToClass { get; }
        public string FromMethod { get; }
        public string ToMethod { get; }
        public bool IsProperty { get; }

        public ClassMap(string fromClass, string toClass, string fromMethod, string toMethod)
        {
            this.FromClass = fromClass;
            this.ToClass = toClass;
            this.FromMethod = fromMethod;
            IsProperty = ToMethod.EndsWith("()");
            this.ToMethod = toMethod.TrimEnd('(',')');
        }
        public ClassMap(string @fromClass, string fromMethod, string toMethod) : this(@fromClass, null, fromMethod, toMethod)
        {
        }
    }
}