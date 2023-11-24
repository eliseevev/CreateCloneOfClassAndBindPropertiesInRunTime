using System.Text.Json;

namespace CreateCloneOfClassAndBindPropertiesInRunTime
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var model = new TestModel() { Name = "some_name", Description = "some_desc" };

            var cloner = new Cloner<TestModel>();
            var clone = cloner.CreateCopy(model);


            Console.WriteLine("Model - " + JsonSerializer.Serialize(model));
            Console.WriteLine("Clone - " + JsonSerializer.Serialize(clone));
        }
    }

    internal class TestModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public int Number { get; set; }
    }
}