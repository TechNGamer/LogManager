using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Text;
using System.Threading;

namespace Utilities.Log {
	// This tells the LogManager where to put the messages it recieves without having 3 seperate methods.
	internal enum MessageStatus {
		Verbose = 0,
		Error = 1,
		Exception = 2
	}

	// This class manages the log files.
	internal class LogManager {
		#region Static

		// Where internal classes can get the singleton of this class from.
		public static LogManager Singleton {
			get {
				if ( singleton is null ) { // Checks to see if the singleton has not been made.
					singleton = new LogManager(); // Assigns a new LogManager as a singleton.
				}

				return singleton; // Returns the singleton.
			}
		}

		public static readonly string LogFolder = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "Log" ); // Where the log folder is at.
		public static readonly string VerboseLog = Path.Combine( LogFolder, "verbose.log" ); // Where the verbose log is at.
		public static readonly string ErrorLog = Path.Combine( LogFolder, "error.log" ); // Where the error log it at.
		public static readonly string ExceptionLog = Path.Combine( LogFolder, "exception.log" ); // Where the exception log is at.
		public static readonly Encoding LogEncoding = Encoding.UTF8; // What encoding it uses.

		private static LogManager singleton; // The singleton.

		#endregion

		// The reference to the thread.
		private Thread logThread;
		// Flags for the thread when it is created so it knows when to become active and start writting and/or exiting.
		private ManualResetEvent waiting = new ManualResetEvent( false ),
								 normalMessageWaiting = new ManualResetEvent( false ),
								 errorMessageWaiting = new ManualResetEvent( false ),
								 exceptionMessageWaiting = new ManualResetEvent( false ),
								 terminate = new ManualResetEvent( false );
		// Queue's that will hold the messages until they are ready to be written.
		private Queue<string> normalMessageQueue = new Queue<string>(),
							  errorMessageQueue = new Queue<string>(),
							  exceptionMessageQueue = new Queue<string>();
		// The file streams that will write to their respective files.
		private FileStream verboseStream, errorStream, exceptionStream;

		// Private constructor allows for the object to only have one of it exist.
		private LogManager() {
			logThread = new Thread( () => WrittingThreadMethod() ) {
				Priority = ThreadPriority.Lowest
			};

			if ( !Directory.Exists( LogFolder ) ) { // Checks to see if the log folder is already made.
				Directory.CreateDirectory( LogFolder ); // Creates the log folder if it has not been made.
			}

			verboseStream = new FileStream( VerboseLog, FileMode.Create ); // Opens the verbose file for writing.
			errorStream = new FileStream( ErrorLog, FileMode.Create ); // Opens the error file for writing.
			exceptionStream = new FileStream( ExceptionLog, FileMode.Create ); // Opens the exception file for writing.

			AppDomain.CurrentDomain.ProcessExit += StopLogging;

			logThread.Start();
		}

		// Used to automatically kill the logging thread.
		private void StopLogging( object sender, EventArgs e ) {
			terminate.Set(); // Set's a flag that the thread should close.
		}

		// Queue's up the messages for writting later by the logging thread.
		public void QueueMessage( string message, MessageStatus status ) {
			message = $"{DateTime.Now.ToString( "MM/dd/yy HH:mm:ss" )} {message}\n"; // Rewrites the message to include the date and time.

			switch ( status ) {
				case MessageStatus.Verbose:
					lock ( normalMessageQueue ) { // Locks the queue so it can queue the message up.
						normalMessageQueue.Enqueue( message );
					}

					normalMessageWaiting.Set(); // Raises the flag.
					break;
				case MessageStatus.Error:
					lock ( errorMessageQueue ) { // Locks the queue so it can queue the message up.
						errorMessageQueue.Enqueue( message );
					}

					errorMessageWaiting.Set(); // Raises the flag.
					break;
				case MessageStatus.Exception:
					lock ( exceptionMessageQueue ) { // Locks the queue so it can queue the message up.
						exceptionMessageQueue.Enqueue( message );
					}

					exceptionMessageWaiting.Set(); // Raises the flag.
					break;
				default:
					break;
			}
		}

		// Used to tell the logging thread to close.
		[Obsolete( "This method is obsolete as the LogManager now subscribes to AppDomain.CurrentDomain.ProcessExit." )]
		public void SignalThreadToClose() {
			terminate.Set(); // Set's a flag that the thread should close.
		}

		// Forces the thread to close.
		public void ForceThreadToClose() {
			logThread.Abort(); // Aborts the thread.
		}

		#region Thread Methods

		// This method, as the name implys, is the method that will be running on a seperate thread.
		private void WrittingThreadMethod() {
			int i;

			while ( true ) { // Infinite loop motherfucker, one is needed here.

				// Waits until a flag is raised with one of these.
				i = WaitHandle.WaitAny( new WaitHandle[] { normalMessageWaiting, errorMessageWaiting, exceptionMessageWaiting, terminate } );

				// Sees which one of these raised.
				switch ( i ) {
					case 0:
						WriteToStream( verboseStream, normalMessageQueue ); // Passes the verbose stream and the normal message queue to be locked and written to file.
						normalMessageWaiting.Reset(); // Resets the flag.
						break;
					case 1:
						WriteToStream( errorStream, errorMessageQueue ); // Passes the error stream and the error message queue to be locked and written to file.
						errorMessageWaiting.Reset(); // Resets the flag.
						break;
					case 2:
						WriteToStream( exceptionStream, exceptionMessageQueue ); // Passes the exception stream and the exception message queue to be locked and written to file.
						exceptionMessageWaiting.Reset(); // Resets the flag.
						break;
					case 3:
						WriteToStream( verboseStream, normalMessageQueue ); // Passes the verbose stream and the normal message queue to be locked and written to file.
						WriteToStream( errorStream, errorMessageQueue ); // Passes the error stream and the error message queue to be locked and written to file.
						WriteToStream( exceptionStream, exceptionMessageQueue ); // Passes the exception stream and the exception message queue to be locked and written to file.
						return; // Used to break the infinite loop and terminate the thread.
					default:
						break;
				}
			}
		}

		// Used to lock the queue and write what's in the queue to the thread.
		private void WriteToStream( FileStream stream, Queue<string> dequeueFrom ) {
			string message;
			byte[] messageBytes;

			lock ( dequeueFrom ) { // Locks the queue so it can dequeue things off of it.
				while ( dequeueFrom.Count > 0 ) { // Loops through the queue until it's empty.
					message = dequeueFrom.Dequeue();

					messageBytes = LogEncoding.GetBytes( message ); // Get's the byte[] that the message will be.
					stream.Write( messageBytes, 0, messageBytes.Length ); // Has the stream write the message to the queue.

//#if DEBUG
//					Debug.WriteLine( $"Writing message '{message}' to file '{stream.Name}'." );
//#endif
				}
			}

			stream.Flush( true ); // This is needed for .NET Core because with .NET Framework, it writes to the file.
		}

		#endregion
	}
}
