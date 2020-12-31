using System;

namespace Converter
{
    public class PriorityAttribute : Attribute
    {
        public int Priority { get; set; }
    }
}