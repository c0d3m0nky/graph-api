using System;

namespace AADB2C.GraphApi.PutOnNuget.ConsoleOptions
{
    public class Option<T>
    {
        public Option(string description, T value, ConsoleColor? color = null, bool show = true)
        {
            Description = description.Trim();
            Value = value;
            Color = color;
            Show = show;
        }

        public string Description { get; set; }
        public T Value { get; set; }
        public ConsoleColor? Color { get; set; }
        public bool Show { get; set; }
    }
}