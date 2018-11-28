using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Reflection;
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

		public static string LogFolder { // Where the log folder is at.
			get;
			private set;
		}

		public static string VerboseLog { // Where the verbose log is at.
			get;
			private set;
		}

		public static string ErrorLog { // Where the error log it at.
			get;
			private set;
		}

		public static string ExceptionLog { // Where the exception log is at.
			get;
			private set;
		}

		public static readonly Encoding LogEncoding = Encoding.BigEndianUnicode; // What encoding it uses.

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
			string programName = Assembly.GetEntryAssembly().GetName().Name;

			logThread = new Thread( () => WrittingThreadMethod() ) { // Creates a new Thread that calls WrittingThreadMethod().
				Priority = ThreadPriority.Lowest // Set's the thread to lowest priority to prevent blocking.
			};

			SetUpLogs( programName ); // See method's doc comment.

			// Opens the verbose file for writing and allows other programs to read the file while the program is able to write to it.
			verboseStream = new FileStream( VerboseLog, FileMode.Create, FileAccess.Write, FileShare.Read );
			// Opens the error file for writing and allows other programs to read the file while the program is able to write to it.
			errorStream = new FileStream( ErrorLog, FileMode.Create, FileAccess.Write, FileShare.Read );
			// Opens the exception file for writing and allows other programs to read the file while the program is able to write to it.
			exceptionStream = new FileStream( ExceptionLog, FileMode.Create, FileAccess.Write, FileShare.Read );

			AppDomain.CurrentDomain.ProcessExit += StopLogging; // Enables automatic execution of calling to stop the logging thread.

			logThread.Start(); // Starts the logging thread.
		}

		/// <summary>
		/// Set's up the log files and folder for use.
		/// </summary>
		/// <param name="programName">The name of the program.</param>
		private void SetUpLogs( in string programName ) {
			if ( Environment.OSVersion.Platform == PlatformID.Unix ) { // Checks to see if the platform is Unix/Unix-like.
				LogFolder = Path.Combine( "/var/log", programName ); // Creates the log folder at /var/log/[program name].
			} else if ( Environment.OSVersion.Platform == PlatformID.Win32NT ) { // Checks to see if the platform is Windows.
				LogFolder = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), programName, "logs" );
			} else { // Otherwise, if it is an unkown platform, create the log folder at the executable location.
				LogFolder = Path.Combine( Assembly.GetEntryAssembly().Location, "logs" );
			}

			VerboseLog = Path.Combine( LogFolder, "verbose.log" ); // Creates the path for the verbose log file.
			ErrorLog = Path.Combine( LogFolder, "error.log" ); // Creates the path for the error log file.
			ExceptionLog = Path.Combine( LogFolder, "exception.log" ); // Creates the path for the exception log fille.

			if ( !Directory.Exists( LogFolder ) ) { // Checks to see if the log folder is already made.
				Directory.CreateDirectory( LogFolder ); // Creates the log folder if it has not been made.
			}
		}

		// Used to automatically kill the logging thread.
		private void StopLogging( object sender, EventArgs e ) {
			terminate.Set(); // Set's a flag that the thread should close.
		}

		// Queue's up the messages for writting later by the logging thread.
		public void QueueMessage( string message, MessageStatus status ) {
			message = $"{DateTime.Now.ToString( "MM/dd/yyyy HH:mm:ss" )} {message}\n"; // Rewrites the message to include the date and time.

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
		[Obsolete( "LogManager now subscribes to AppDomain.CurrentDomain.ProcessExit. The reason it is staying here is for backwards compatibility." )]
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
			int waitKey;

			while ( true ) { // Infinite loop motherfucker, one is needed here.

				// Waits until a flag is raised with one of these.
				waitKey = WaitHandle.WaitAny( new WaitHandle[] { normalMessageWaiting, errorMessageWaiting, exceptionMessageWaiting, terminate } );

				// Sees which one of these raised.
				switch ( waitKey ) {
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

#if DEBUG
						Debug.WriteLine( $"Writing message '{message}' to file '{stream.Name}'." );
#endif
				}
			}

			stream.Flush( true );
		}

		#endregion
	}
}
