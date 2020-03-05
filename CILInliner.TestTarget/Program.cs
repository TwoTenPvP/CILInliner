using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace CILInliner.TestTarget
{
    class Program
    {
        static void Main(string[] args)
        {
            SoftwareInlined();
            NotInlined();

            Console.WriteLine(myX);

            new Ins().MyLocal();
            new Ins2().MyLocal();
            new Ins3().MyLocal();

            SealedClass o = new SealedClass();
            o.Equals("Rubber ducky");
            o.Method();
        }


        private static int myX = 10;

        public static void PreInlined()
        {
            int x = 0;
            int y = 0;
            int elements = myX;
            IPAddress addr = IPAddress.Parse("127.0.0.1");

            for (int i = 0; i < elements; i++)
            {
                Console.WriteLine(i);
            }

            myX += elements;

            Console.WriteLine(addr.ToString());

            if (elements == 10)
            {
                Console.WriteLine("1");
            }
            else
            {
                Console.WriteLine("2");
            }
        }

        public static void SoftwareInlined()
        {
            int x = 0;
            int y = 0;
            bool add = Add(myX, IPAddress.Parse("127.0.0.1"));
            if (add)
            {
                Console.WriteLine("1");
            }
            else
            {
                Console.WriteLine("2");
            }
        }

        public static void NotInlined()
        {
            int x = 0;
            int y = 0;
            bool add = Add2(myX, IPAddress.Parse("127.0.0.1"));
            if (add)
            {
                Console.WriteLine("1");
            }
            else
            {
                Console.WriteLine("2");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Add(int elements, IPAddress addr)
        {
            myX += elements;

            Console.WriteLine(addr.ToString());

            for (int i = 0; i < elements; i++)
            {
                Console.WriteLine(i);
            }

            if (elements == 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool Add2(int elements, IPAddress addr)
        {
            myX += elements;

            Console.WriteLine(addr.ToString());

            for (int i = 0; i < elements; i++)
            {
                Console.WriteLine(i);
            }

            if (elements == 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void CallInstance()
        {
            Ins ins = new Ins();
            ins.MyMeth(10);
        }
    }

    public class Ins
    {
        public int x = 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MyMeth(int y)
        {
            int z = 20;
            float mmmm = 200;

            int k = x;

            if (x == y)
            {
                Console.WriteLine("Same");
            }
            else
            {
                Console.WriteLine("Diff");
            }

            float mmm = 200;
        }

        public void MyLocal()
        {
            float m = 200;
            MyMeth(10);
            float mm = 230;
        }
    }

    public class Ins2
    {
        public int x = 10;

        public void MyMeth(int y)
        {
            int z = 20;
            float mmmm = 200;

            int k = x;

            if (x == y)
            {
                Console.WriteLine("Same");
            }
            else
            {
                Console.WriteLine("Diff");
            }
            float mmm = 200;
        }

        public void MyLocal()
        {
            float m = 200;
            MyMeth(10);
            float mm = 230;
        }
    }

    public class Ins3
    {
        public int x = 10;

        public void MyMeth(int y)
        {
            int z = 20;
            float mmmm = 200;

            int k = x;

            if (x == y)
            {
                Console.WriteLine("Same");
            }
            else
            {
                Console.WriteLine("Diff");
            }
            float mmm = 200;
        }

        public void MyLocal()
        {
            float m = 200;
            int y = 10;
            int z = 20;
            float mmmm = 200;

            int k = x;

            if (x == y)
            {
                Console.WriteLine("Same");
            }
            else
            {
                Console.WriteLine("Diff");
            }
            float mmm = 200;
            float mm = 230;
        }
    }

    public sealed class SealedClass
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Method()
        {
            Console.WriteLine("Hello");
        }
    }
}
