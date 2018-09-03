using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utilities.Log {
	/// <summary>
	/// This class acts as a middle man to the log files.
	/// This enables the logs to help tell the person looking at it where a message came from.
	/// All messages will be passed to the actual log manager with this format: mm/dd/yy HH:mm:ss '[Full name of class' said: [message]
	/// </summary>
	public class LogTalker {

		/// <summary>
		/// The name of the object that this object will be representing in the logs.
		/// </summary>
		public string RepresentName {
			get;
			private set;
		}

		private LogManager logManager; // The reference to the singleton.

		/// <summary>
		/// Creates a new LogTalker for the Type passed to it.
		/// </summary>
		/// <param name="t">The type of the object.</param>
		public LogTalker( Type t ) {
			RepresentName = t.FullName;
			logManager = LogManager.Singleton;
		}

		/// <summary>
		/// Creates a new LogTalker for the object passed to it.
		/// This constructor makes it more specific to which object by also including the objects hash code.
		/// </summary>
		/// <param name="o">The object you wish to be represented.</param>
		public LogTalker( object o ) : this(o.GetType()) {
			RepresentName = $"{RepresentName}@{o.GetHashCode()}";
		}

		/// <summary>
		/// Writes any messages passed to it to the verbose log.
		/// </summary>
		/// <param name="message">The message that will be written to verbose.log.</param>
		public void WriteVerbose( string message ) {
			// Calls the QueueMessage method of LogManager to queue the message to the verbose log file.
			logManager.QueueMessage( $"'{RepresentName}' said: {message}", MessageStatus.Verbose );
		}

		/// <summary>
		/// Writes any messages passed to it to the error log.
		/// </summary>
		/// <param name="message">The message that will be written to error.log.</param>
		public void WriteError( string message ) {
			// Calls the QueueMessage method of LogManager to queue the message to the error log file.
			logManager.QueueMessage( $"'{RepresentName}' said: {message}", MessageStatus.Error );
		}

		/// <summary>
		/// Writes any messages passed to it to the exception log.
		/// By default, <paramref name="crash"/> is default true, meaning that it will print the message to the console if present.
		/// </summary>
		/// <param name="message">The message the will be written to exception.log.</param>
		/// <param name="crash">A boolean for printing the exception message to the console or not and exit the program.</param>
		public void WriteException( string message, bool crash = true ) {
			// Calls the QueueMessage method of LogManager to queue the message to the exception log file.
			logManager.QueueMessage( $"'{RepresentName}' said: {message}", MessageStatus.Exception );

			if( crash ) { // Checks to see if the caller wants the program to crash after queueing up the message.
				FailFast( message ); // See the method's comment for info on this.
			}
		}

		/// <summary>
		/// Writes any messages passed to it to the exception log.
		/// By default, <paramref name="crash"/> is default true, meaning that it will print the message to the console if present.
		/// <paramref name="e"/> will be incorperated into the message before being written to the log file.
		/// <seealso cref="WriteException(string, bool)"/>
		/// </summary>
		/// <param name="message">The message the will be written to exception.log.</param>
		/// <param name="e">The exception that will be incorperated into the message.</param>
		/// <param name="crash">A boolean for printing the exception message to the console or not.</param>
		public void WriteException( string message, Exception e, bool crash = true ) {
			// Calls the original method with the message this method got being incorperated with the exception and being passed as a new message.
			WriteException( $"{message}\nException Message: {e.Message}\nStacktrace:\n{e.StackTrace}", crash );
		}

		/// <summary>
		/// Tells the object to stop writing to the thread.
		/// </summary>
		public void Close() {
			logManager.SignalThreadToClose();
		}

		// This method's purpose it to write out the message then exit the program.
		private void FailFast( string message ) {
			Console.ForegroundColor = ConsoleColor.Red; // This is to show that something happened.
			Console.BackgroundColor = ConsoleColor.Black; // This is to help the contrast.
			Console.WriteLine( message ); // Writes the message out and ending it with a definitive new line.
			Console.ResetColor(); // Resets the colors back.
			Console.Write( "Please press any key to quit. . ." ); // Tells the user to press any key to make the program quit.
			Console.ReadKey( true ); // Tells the console to hand it which key is being pressed and to not print that key out to the console.
			Environment.Exit( -1 ); // Exits and passes -1 to indicate that the program failed.
		}
	}
}
