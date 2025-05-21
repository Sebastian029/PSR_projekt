using System;

namespace ServerDst
{
    public class Class1
    {
        public void Run()
        {
            Console.WriteLine("Class1 is running!");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting the program...");

            Class1 myClass = new Class1();
            myClass.Run();

            Console.WriteLine("Program finished.");
        }
    }
}