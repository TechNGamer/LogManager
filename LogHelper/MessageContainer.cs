using System;
using System.Collections.Generic;

namespace Logging {
	/// <summary>
	/// Message is used to hold information regarding the message it recived.
	/// </summary>
	public struct MessageContainer : IEquatable<MessageContainer> {
		/// <summary>
		/// From is a string that holds which class it came from or what object with the memory location.
		/// </summary>
		public string From {
			get;
		}

		/// <summary>
		/// Holds the actual message.
		/// </summary>
		public string Message {
			get;
		}

		/// <summary>
		/// The date and time it was pushed to the LogManager.
		/// </summary>
		public DateTime Recieved {
			get;
		}

		/// <summary>
		/// Constructor to build this immutable struct.
		/// </summary>
		/// <param name="from">What class/object is it from.</param>
		/// <param name="message">The message produced by the object/class.</param>
		/// <param name="recieved">The date and time the message was recived.</param>
		public MessageContainer( string from, string message, DateTime recieved ) {
			From = from;
			Message = message;
			Recieved = recieved;
		}

		/// <summary>
		/// Compares to see if an object is the same as this object.
		/// </summary>
		/// <param name="obj">The object to check.</param>
		/// <returns>True if the objects are the same, otherwise false.</returns>
		public override bool Equals( object obj ) {
			return obj is MessageContainer && Equals( ( MessageContainer )obj );
		}

		/// <summary>
		/// Checks to see if another MessageContainer is the same as this one.
		/// </summary>
		/// <param name="other">Another MessageContainer.</param>
		/// <returns>True if they have the same contents.</returns>
		public bool Equals( MessageContainer other ) {
			return From == other.From && Message == other.Message && Recieved == other.Recieved;
		}

		/// <summary>
		/// Generates a hash code for this struct.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode() {
			int hashCode = 1733014643;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode( From );
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode( Message );
			hashCode = hashCode * -1521134295 + Recieved.GetHashCode();
			return hashCode;
		}

		/// <summary>
		/// Compares to see if an object is the same as a MessageContainer.
		/// See also: <seealso cref="Equals(object)"/>.
		/// </summary>
		/// <param name="left">The MessageContainer.</param>
		/// <param name="right">The object to check.</param>
		/// <returns>True if they are the same.</returns>
		public static bool operator ==( MessageContainer left, object right ) {
			return left.Equals( right );
		}

		/// <summary>
		/// Compares to see if they are the same.
		/// See also: <seealso cref="Equals(MessageContainer)"/>.
		/// </summary>
		/// <param name="left">The left hand side MessageContainer.</param>
		/// <param name="right">The right hand side MessageContainer.</param>
		/// <returns>True if they are the same.</returns>
		public static bool operator ==( MessageContainer left, MessageContainer right ) {
			return left.Equals( right );
		}

		/// <summary>
		/// Compares to see if an object is not the same as a MessageContainer.
		/// </summary>
		/// <param name="left">The MessageContainer.</param>
		/// <param name="right">The object to check.</param>
		/// <returns>True if they are not the same.</returns>
		public static bool operator !=( MessageContainer left, object right ) {
			return !left.Equals( right );
		}

		/// <summary>
		/// Compares to see if they are not the same.
		/// See also: <seealso cref="Equals(object)"/>.
		/// </summary>
		/// <param name="left">The left hand side MessageContainer.</param>
		/// <param name="right">The right hand side MessageContainer.</param>
		/// <returns>True if they are not the same.</returns>
		public static bool operator !=( MessageContainer left, MessageContainer right ) {
			return !left.Equals( right );
		}
	}
}