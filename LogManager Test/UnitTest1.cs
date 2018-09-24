using System;
using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Utilities.Log;

namespace LogHelperTest {
	[TestClass]
	public class UnitTest1 {
		[TestMethod]
		public void LogTalkerTest() {
			LogTalker log = new LogTalker( this, false );
			Stopwatch stopWatch = Stopwatch.StartNew();

			Console.WriteLine( $"Rep name: {log.RepresentName}" );

			log.WriteVerbose( "Testing verbose." );
			log.WriteError( "This is an error." );
			log.WriteException( "This is an exception bitch.", false );
			log.WriteVerbose( "Just kidding. Still going to test crash." );

			stopWatch.Start();
			DateTime now = DateTime.Now;

			for ( uint i = 0; i <= 37500; ++i ) {
				log.WriteVerbose( Convert.ToString( i, 2 ) );
			}

			stopWatch.Stop();

			log.WriteVerbose( $"It took {stopWatch.ElapsedMilliseconds / 1000} seconds. Start time was {now.ToString( "HH:mm:ss" )}, end time was {DateTime.Now.ToString( "HH:mm:ss" )}." );
		}
	}
}