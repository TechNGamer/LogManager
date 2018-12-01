using System;
using System.Diagnostics;

using NUnit.Framework;

using Utilities.Log;

namespace Tests {
	public class Tests {

		private LogTalker log;

		[SetUp]
		public void Setup() {
			log = new LogTalker( this );
		}

		[Test]
		public void VerboseTest() {
			log.WriteVerbose( "This is a verbose test." );
		}

		[Test]
		public void ErrorTest() {
			log.WriteError( "This is an error test." );
		}

		[Test]
		public void ExceptionTest() {
			log.WriteException( "This is an exception test.", new Exception( "This is a test." ), false );
		}

		[Test]
		public void OutputTest() {
			log.WritingMessage += TestWritingMessage;

			log.WriteVerbose( "This is a test of WritingMessage" );
		}

		private void TestWritingMessage( string message, MessageStatus status ) {
			Debug.WriteLine( $"Got message: {message}\nIt came with status: {Enum.GetName( status.GetType(), status )}" );
		}
	}
}