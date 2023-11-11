using System;

namespace Ugol.BehaviourTree
{
	// inspired by active logic
	public readonly struct Status
	{
		private readonly int _w;

		private Status(int w) => this._w = w;


		public static readonly Status COMPLETE = new Status(+1);
		public static readonly Status FAILED = new Status(-1);
		public static readonly Status RUNNING = new Status(0);

		
		public bool failed => this._w == -1;
		public bool running => this._w == 0;
		public bool complete => this._w == +1;


		// Sequence/Selector - these are used for && and || operators to work properly, do not use them directly

		// x && y evaluates as Status.false(x) ? x : Status.operator&(x, y)
		public static bool operator false(Status s) => s._w != +1;
		public static Status operator &(Status x, Status y) => y;

		// x || y evaluates as Status.true(x) ? x : Status.operator|(x, y)
		public static bool operator true(Status s) => s._w != -1;
		public static Status operator |(Status x, Status y) => y;


		// Other operators

		public static Status operator +(Status x, Status y) => new Status(Math.Max(x._w, y._w));

		public static Status operator *(Status x, Status y) => new Status(Math.Min(x._w, y._w));

		public static Status operator %(Status x, Status y) => new Status(x._w);

		public static Status operator !(Status s) => new Status(-s._w);

		public static Status operator ~(Status s) => new Status(s._w * s._w);

		public static Status operator +(Status s) => new Status(s._w == +1 ? +1 : s._w + 1);

		public static Status operator -(Status s) => new Status(s._w == -1 ? -1 : s._w - 1);

		public static Status operator ++(Status s) => +s;

		public static Status operator --(Status s) => -s;


		// Type conversions

		public static implicit operator Status(bool that) => new Status(that ? +1 : -1);


		// Equality and hashing

		public override bool Equals(object x) => x is Status rhs && Equals(rhs);

		public bool Equals(Status x) => this == x;

		public static bool operator ==(Status x, Status y) => x._w == y._w;

		public static bool operator !=(Status x, Status y) => !(x == y);

		public override int GetHashCode() => this._w;

		public override string ToString() => this._w.ToString();
	}
}
