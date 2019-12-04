using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        class MyClass: IDisposable
        {
            private MyOTherClass _inner = new MyOTherClass();

            public void Dispose()
            {
                
            }
        }

        class MyOTherClass: IDisposable
        {
            public void Dispose()
            {
                
            }
        }
    }
}
