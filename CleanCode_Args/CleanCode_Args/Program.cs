using System;

namespace CleanCode_Args
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Args arg = new Args("l,p#,d*", args);
                bool logging = arg.GetBoolean('l');
                int port = arg.GetInt('p');
                string directory = arg.GetString('d');

                Console.WriteLine(string.Format("Logging : {0}", logging));
                Console.WriteLine(string.Format("Port : {0}", port));
                Console.WriteLine(string.Format("Directory : {0}", directory));
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                Console.Read();
            }

        }
    }
}
