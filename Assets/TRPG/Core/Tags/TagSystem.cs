using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
namespace Game.Systems.Tags
{
    public readonly struct TagKey : IEquatable<TagKey>
    {
        public readonly int Id;
        public readonly string Name;

        public TagKey(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public bool Equals(TagKey other) => Id == other.Id;
        public override int GetHashCode() => Id;
        public override string ToString() => Name;
    }
}