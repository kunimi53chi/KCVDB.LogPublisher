using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace KCVDB.LogFilePublisher
{
    struct Hash : IEquatable<Hash>
    {
        public static Hash Compute(string s)
        {
            using (var sha256 = SHA256.Create())
            {
                return new Hash(sha256.ComputeHash(Encoding.UTF8.GetBytes(s)));
            }
        }

        public Hash(string s)
            : this(Convert.FromBase64String(s))
        {
        }

        public Hash(byte[] byteArray)
        {
            this.byteArray = byteArray;
        }

        public override bool Equals(object obj)
        {
            return obj != null && this.Equals((Hash)obj);
        }

        public bool Equals(Hash other)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(this.byteArray, other.byteArray);
        }

        public static bool operator ==(Hash obj1, Hash obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Hash obj1, Hash obj2)
        {
            return !(obj1 == obj2);
        }

        public override int GetHashCode()
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(this.byteArray);
        }

        public override string ToString()
        {
            return Convert.ToBase64String(this.byteArray);
        }

        private readonly byte[] byteArray;
    }
}
