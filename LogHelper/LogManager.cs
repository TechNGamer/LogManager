using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Logging {
	/// <summary>
	/// This tells the LogManager where to put the messages it recieves without having 3 seperate methods.
	/// It also tells methods that are part of the WritingMessage delegate of what type the message is.
	/// </summary>
	public enum MessageStatus {
		/// <summary>
		/// Indicates that the message is just a verbose message.
		/// </summary>
		Verbose = 0,
		/// <summary>
		/// Indicates that the message is an error message.
		/// </summary>
		Error = 1,
		/// <summary>
		/// Indicates that the message came from an exception.
		/// </summary>
		Exception = 2
	}

	/// <summary>
	/// The LogManager handles all communication to the logs.
	/// Please note that this is a singleton, however the singleton is only accessable via classes in the LogHelper assembly.
	/// </summary>
	public sealed class LogManager {

		#region Static

		// Where internal classes can get the singleton of this class from.
		// The internal keyword is used to as a hack to preven people from creating/getting the LogManager.
		internal static LogManager Singleton {
			get {
				if ( singleton is null ) { // Checks to see if the singleton has not been made.
					singleton = new LogManager(); // Assigns a new LogManager as a singleton.
				}

				return singleton; // Returns the singleton.
			}
		}

		/// <summary>
		/// The location of the log folder.
		/// </summary>
		public static string LogFolder { // Where the log folder is at.
			get;
			private set;
		}

		/// <summary>
		/// The path to the verbose log file.
		/// </summary>
		public static string VerboseLog { // Where the verbose log is at.
			get;
			private set;
		}

		/// <summary>
		/// The path to the error log file.
		/// </summary>
		public static string ErrorLog { // Where the error log it at.
			get;
			private set;
		}

		/// <summary>
		/// The path to the error log file.
		/// </summary>
		public static string ExceptionLog { // Where the exception log is at.
			get;
			private set;
		}

		/// <summary>
		/// Get's the encoding type the log files are written in.
		/// </summary>
		public static readonly Encoding LogEncoding = Encoding.BigEndianUnicode; // What encoding it uses.

		private static LogManager singleton; // The singleton.

		#endregion

		// The reference to the thread.
		private Thread logThread;
		// Flags for the thread when it is created so it knows when to become active and start writting and/or exiting.
		private ManualResetEvent normalMessageWaiting = new ManualResetEvent( false ),
								 errorMessageWaiting = new ManualResetEvent( false ),
								 exceptionMessageWaiting = new ManualResetEvent( false ),
								 terminate = new ManualResetEvent( false );
		// Queue's that will hold the messages until they are ready to be written.
		private Queue<MessageContainer> normalMessageQueue = new Queue<MessageContainer>(),
							  errorMessageQueue = new Queue<MessageContainer>(),
							  exceptionMessageQueue = new Queue<MessageContainer>();
		// The file streams that will write to their respective files.
		private FileStream verboseStream, errorStream, exceptionStream;

		/// <summary>
		/// This delegate provides the message and the type of the message to all methods.
		/// </summary>
		/// <param name="message">The message that was printed out.</param>
		/// <param name="status">THe kind of message it was.</param>
		public delegate void WritingMessage( MessageContainer message, MessageStatus status );

		/// <summary>
		/// Used to signal to classes/objects that the log manager has written the message to the logs.
		/// </summary>
		public WritingMessage writingMessage;

		/// <summary>
		/// This static constructor set's up the entire class before LogManager is instantiated.
		/// </summary>
		static LogManager() {
			string programName = Assembly.GetEntryAssembly().GetName().Name;

#if !DEBUG
			if ( Environment.OSVersion.Platform == PlatformID.Unix ) { // Checks to see if the platform is Unix/Unix-like.
				LogFolder = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), $".{programName.ToLower()}", "logs" );
			} else if ( Environment.OSVersion.Platform == PlatformID.Win32NT ) { // Checks to see if the platform is Windows.
				LogFolder = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), programName, "logs" );
			} else { // Otherwise, if it is an unkown platform, create the log folder at the executable location.
				LogFolder = Path.Combine( Assembly.GetEntryAssembly().Location, "logs" );
			}
#else
			LogFolder = Path.Combine( Path.GetDirectoryName( Assembly.GetEntryAssembly().FullName ), "Logs" );
#endif


			VerboseLog = Path.Combine( LogFolder, "verbose.log" ); // Creates the path for the verbose log file.
			ErrorLog = Path.Combine( LogFolder, "error.log" ); // Creates the path for the error log file.
			ExceptionLog = Path.Combine( LogFolder, "exception.log" ); // Creates the path for the exception log fille.

			if ( !Directory.Exists( LogFolder ) ) { // Checks to see if the log folder is already made.
				Directory.CreateDirectory( LogFolder ); // Creates the log folder if it has not been made.
			}
		}

		// Private constructor allows for the object to only have one of it exist.
		private LogManager() {
			logThread = new Thread( () => WrittingThreadMethod() ) { // Creates a new Thread that calls WrittingThreadMethod().
				Priority = ThreadPriority.Lowest // Set's the thread to lowest priority to prevent blocking.
			};

			//SetUpLogs( programName ); // See method's doc comment.

			// Opens the verbose file for writing and allows other programs to read the file while the program is able to write to it.
			verboseStream = new FileStream( VerboseLog, FileMode.Create, FileAccess.Write, FileShare.Read );
			// Opens the error file for writing and allows other programs to read the file while the program is able to write to it.
			errorStream = new FileStream( ErrorLog, FileMode.Create, FileAccess.Write, FileShare.Read );
			// Opens the exception file for writing and allows other programs to read the file while the program is able to write to it.
			exceptionStream = new FileStream( ExceptionLog, FileMode.Create, FileAccess.Write, FileShare.Read );

			AppDomain.CurrentDomain.ProcessExit += StopLogging; // Enables automatic execution of calling to stop the logging thread.

			logThread.Start(); // Starts the logging thread.
		}

		// Used to automatically kill the logging thread.
		private void StopLogging( object sender, EventArgs e ) {
			terminate.Set(); // Set's a flag that the thread should close.
		}

		// Queue's up the messages for writting later by the logging thread.
		internal void QueueMessage( string logClass, string message, MessageStatus status ) {
			MessageContainer incomingMessage = new MessageContainer( logClass, message, DateTime.Now ); // Creates the immutable struct.

			switch ( status ) {
				case MessageStatus.Verbose:
					lock ( normalMessageQueue ) { // Locks the queue so it can queue the message up.
						normalMessageQueue.Enqueue( incomingMessage );
					}

					normalMessageWaiting.Set(); // Raises the flag.
					break;
				case MessageStatus.Error:
					lock ( errorMessageQueue ) { // Locks the queue so it can queue the message up.
						errorMessageQueue.Enqueue( incomingMessage );
					}

					errorMessageWaiting.Set(); // Raises the flag.
					break;
				case MessageStatus.Exception:
					lock ( exceptionMessageQueue ) { // Locks the queue so it can queue the message up.
						exceptionMessageQueue.Enqueue( incomingMessage );
					}

					exceptionMessageWaiting.Set(); // Raises the flag.
					break;
				default:
					break;
			}
		}

		// Used to tell the logging thread to close.
		[Obsolete( "LogManager now subscribes to AppDomain.CurrentDomain.ProcessExit. The reason it is staying here is for backwards compatibility." )]
		internal void SignalThreadToClose() {
			terminate.Set(); // Set's a flag that the thread should close.
		}

		// Forces the thread to close.
		internal void ForceThreadToClose() {
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
		private void WriteToStream( FileStream stream, Queue<MessageContainer> queue ) {
			string message;
			byte[] messageBytes;
			MessageStatus status;
			MessageContainer queuedMessage;

			lock ( queue ) { // Locks the queue so it can dequeue things off of it.

				if ( queue == normalMessageQueue ) { // Checks to see if queue is the normal/verbose queue.
					status = MessageStatus.Verbose; // Applies the verbose status.
				} else if ( queue == errorMessageQueue ) { // Checks to see if queue is the error queue.
					status = MessageStatus.Error; // Applies the error status.
				} else {
					status = MessageStatus.Exception; // Assumes the queue is the exception queue.
				}

				while ( queue.Count > 0 ) { // Loops through the queue until it's empty.
					queuedMessage = queue.Dequeue();

					message = $"'{queuedMessage.From}' @ {queuedMessage.Recieved.ToString( "MM/dd/yyyy HH:mm:ss" )} said: {queuedMessage.Message}\n";

					messageBytes = LogEncoding.GetBytes( message ); // Get's the byte[] that the message will be.
					stream.Write( messageBytes, 0, messageBytes.Length ); // Has the stream write the message to the queue.

					try {
						writingMessage( queuedMessage, status ); // Calls upon this delegate.
					} catch {

					}
				}
			}

			stream.Flush( true ); // Flushes the stream to the file.
		}

		#endregion
	}
}
