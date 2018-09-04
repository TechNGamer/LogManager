using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Log;

namespace LogWritterUnitTest {
	[TestClass]
	public class UnitTest1 {
		[TestMethod]
		public void TestLogger() {
			LogTalker log = new LogTalker( this );

			log.WriteVerbose( "*notices buldge*" );
			log.WriteError( "OwO What's this?" );
			log.WriteException( "UwU I didn't mwen to ovew stwep.", false );

			//log.Close(); See the obsolete message.
			LogTalker.FinishWriting();
		}
	}
}