using System;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal struct TagKey : IEquatable<TagKey> {

        public readonly FixedCharacterSpan topLevelName;
        public readonly FixedCharacterSpan localName;

        public TagKey(FixedCharacterSpan topLevelName, FixedCharacterSpan localName = default) {
            this.topLevelName = topLevelName;
            this.localName = localName;
        }

        public bool Equals(TagKey other) {
            return topLevelName.Equals(other.topLevelName) && localName.Equals(other.localName);
        }

        public override bool Equals(object obj) {
            return obj is TagKey other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 31 + topLevelName.GetHashCode();
                hash = hash * 31 + localName.GetHashCode();
                return hash;
            }
        }

    }

}