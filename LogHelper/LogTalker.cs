﻿using System;
using System.Text;

namespace Utilities.Log {

	/// <summary>
	/// This class acts as a middle man to the log files.
	/// This enables the logs to help tell the person looking at it where a message came from.
	/// All messages will be passed to the actual log manager with this format: mm/dd/yy HH:mm:ss '[Full name of class' said: [message]
	/// </summary>
	public class LogTalker {

		#region Static

		/// <summary>
		/// Exposes the log folder location.
		/// </summary>
		public static string LogFolder {
			get {
				return LogManager.LogFolder;
			}
		}

		/// <summary>
		/// Exposes the verbose.log file location.
		/// </summary>
		public static string VerboseLog {
			get {
				return LogManager.VerboseLog;
			}
		}

		/// <summary>
		/// Exposes the verbose.log file location.
		/// </summary>
		public static string ErrorLog {
			get {
				return LogManager.ErrorLog;
			}
		}

		/// <summary>
		/// Exposes the error.log file location.
		/// </summary>
		public static string ExceptionLog {
			get {
				return LogManager.ExceptionLog;
			}
		}

		/// <summary>
		/// Exposes the exception.log file location.
		/// </summary>
		public static Encoding LogEncoding {
			get {
				return LogManager.LogEncoding;
			}
		}

		/// <summary>
		/// Tells LogManager to signal to the logging thread to finish writing and terminate.
		/// </summary>
		public static void FinishWriting() {
			LogManager.Singleton.SignalThreadToClose();
		}

		#endregion

		/// <summary>
		/// The name of the object that this object will be representing in the logs.
		/// </summary>
		public string RepresentName {
			get;
			private set;
		}

		private LogManager logManager; // The reference to the singleton.
		private bool printToConsole;

		/// <summary>
		/// Creates a new LogTalker for the Type passed to it.
		/// </summary>
		/// <param name="t">The type of the object.</param>
		/// <param name="printToConsole">A boolean for rather every call will print to the console.</param>
		public LogTalker( Type t, bool printToConsole = false ) {
			RepresentName = t.FullName;
			logManager = LogManager.Singleton;

			this.printToConsole = printToConsole;

			logManager.QueueMessage( $"'{GetType().FullName}' representing '{RepresentName}' is up.", MessageStatus.Verbose );
		}

		/// <summary>
		/// Creates a new LogTalker for the object passed to it.
		/// This constructor makes it more specific to which object by also including the objects hash code.
		/// </summary>
		/// <param name="o">The object you wish to be represented.</param>
		/// <param name="printToConsole">A boolean for rather every call will print to the console.</param>
		public LogTalker( object o, bool printToConsole = false ) : this( o.GetType(), printToConsole ) {
			RepresentName = $"{RepresentName}@{o.GetHashCode()}";
		}

		/// <summary>
		/// Writes any messages passed to it to the verbose log.
		/// </summary>
		/// <param name="message">The message that will be written to verbose.log.</param>
		public void WriteVerbose( string message ) {
			message = $"'{RepresentName}' said: {message}";

			// Calls the QueueMessage method of LogManager to queue the message to the verbose log file.
			logManager.QueueMessage( message, MessageStatus.Verbose );

			if ( printToConsole ) {
				Console.WriteLine( message );
			}
		}

		/// <summary>
		/// Writes any messages passed to it to the error log.
		/// </summary>
		/// <param name="message">The message that will be written to error.log.</param>
		public void WriteError( string message ) {
			message = $"'{RepresentName}' said: {message}";

			// Calls the QueueMessage method of LogManager to queue the message to the error log file.
			logManager.QueueMessage( message, MessageStatus.Error );

			if ( printToConsole ) {
				Console.WriteLine( message );
			}
		}

		/// <summary>
		/// Writes any messages passed to it to the exception log.
		/// By default, <paramref name="crash"/> is default true, meaning that it will print the message to the console if present.
		/// </summary>
		/// <param name="message">The message the will be written to exception.log.</param>
		/// <param name="crash">A boolean for printing the exception message to the console or not and exit the program.</param>
		public void WriteException( string message, bool crash = true ) {
			message = $"'{RepresentName}' said: {message}";

			// Calls the QueueMessage method of LogManager to queue the message to the exception log file.
			logManager.QueueMessage( message, MessageStatus.Exception );

			if ( crash ) { // Checks to see if the caller wants the program to crash after queueing up the message.
				FailFast( message ); // See the method's comment for info on this.
			}
		}

		/// <summary>
		/// Writes any messages passed to it to the exception log.
		/// By default, <paramref name="crash"/> is default true, meaning that it will print the message to the console if present.
		/// <paramref name="e"/> will be incorperated into the message before being written to the log file.
		/// See also: <seealso cref="WriteException(string, bool)"/>
		/// </summary>
		/// <param name="message">The message the will be written to exception.log.</param>
		/// <param name="e">The exception that will be incorperated into the message.</param>
		/// <param name="crash">A boolean for printing the exception message to the console or not.</param>
		public void WriteException( string message, Exception e, bool crash = true ) {
			// Calls the original method with the message this method got being incorperated with the exception and being passed as a new message.
			WriteException( $"{message}\nException Message: {e.Message}\nStacktrace:\n{e.StackTrace}", crash );
		}

		/// <summary>
		/// Tells the LogManager to signal to the logging thread to write everything and terminate.
		/// </summary>
		[Obsolete( "Use LogTalker.FinishWriting() instead." )]
		public void Close() {
			logManager.SignalThreadToClose();
		}

		// This method's purpose it to write out the message then exit the program.
		private void FailFast( string message ) {
			Console.ForegroundColor = ConsoleColor.Red; // This is to show that something happened.
			Console.BackgroundColor = ConsoleColor.Black; // This is to help the contrast.
			Console.WriteLine( message ); // Writes the message out and ending it with a definitive new line.
		}
	}
}
