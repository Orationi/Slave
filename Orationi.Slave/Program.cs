using System;

namespace Orationi.Slave
{
	class Program
	{
		static void Main(string[] args)
		{
			using (OrationiSlave orationiSlave = new OrationiSlave())
			{
				orationiSlave.Connect();
				Console.WriteLine("Slave is running...");

				Console.ReadKey();

				orationiSlave.Disconnect();
				Console.WriteLine("Disconnected");
				Console.ReadKey();
			}
		}
	}
}
