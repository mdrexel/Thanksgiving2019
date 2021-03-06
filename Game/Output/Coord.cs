﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.FormattableString;

namespace Game.Output
{
    [DebuggerDisplay("({this.X,nq}, {this.Y,nq})")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Coord : IEquatable<Coord>
    {
        public static readonly Coord Zero = new Coord(0, 0);
        public static readonly Coord OneOffset = new Coord(1, 1);

        public readonly short X;
        public readonly short Y;

        public Coord(short X, short Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public static Coord operator +(Coord left, Coord right)
        {
            checked
            {
                return new Coord((short)(left.X + right.X), (short)(left.Y + right.Y));
            }
        }

        public static Coord operator -(Coord left, Coord right)
        {
            checked
            {
                return new Coord((short)(left.X - right.X), (short)(left.Y - right.Y));
            }
        }

        public static Coord operator *(Coord left, Coord right)
        {
            checked
            {
                return new Coord((short)(left.X * right.X), (short)(left.Y * right.Y));
            }
        }

        public static Coord operator *(short left, Coord right)
        {
            checked
            {
                return new Coord((short)(left * right.X), (short)(left * right.Y));
            }
        }

        public static Coord operator *(Coord left, short right)
        {
            checked
            {
                return new Coord((short)(left.X * right), (short)(left.Y * right));
            }
        }

        public static bool operator ==(Coord left, Coord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Coord left, Coord right)
        {
            return !(left == right);
        }
        public override bool Equals(object obj)
        {
            if (obj is Coord other)
            {
                return this.Equals(other);
            }

            return false;
        }

        public Coord Invert()
        {
            return new Coord(this.Y, this.X);
        }

        public bool Equals(Coord other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        public override int GetHashCode()
        {
            int hash = 991;
            hash = (hash * 31) + this.X;
            hash = (hash * 31) + this.Y;

            return hash;
        }

        public override string ToString()
        {
            return Invariant($"({this.X}, {this.Y})");
        }

    };
}
