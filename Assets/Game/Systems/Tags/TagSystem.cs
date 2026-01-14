using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
namespace Game.Systems.Tags
{
    [Serializable]
    public class Tag
    {
        public int id;
        public string name;
        public string category;

        public Tag(int id, string name, string category)
        {
            this.id = id;
            this.name = name;
            this.category = category;
        }
    }

    [Serializable]
    public class TagCategory
    {
        public string categoryName;
        public List<Tag> tags = new();
        public Dictionary<string, List<Tag>> tagsMap = new();

        public TagCategory(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public void AddTag(Tag tag)
        {
            if (!tags.Exists(t => t.id == tag.id)) tags.Add(tag);
        }

        public void AddTag(int id, String tagName)
        {
            var tag = new Tag(id, tagName, categoryName);
            AddTag(tag);
        }

        public void AddSubList(string subListName)
        {
            if (!tagsMap.ContainsKey(subListName)) tagsMap[subListName] = new List<Tag>();
        }

        public void AddTagToSubList(string subListName, int id)
        {
            if (!tagsMap.ContainsKey(subListName)) return;

            var tag = GetTag(id);
            var subList = tagsMap[subListName];

            if (!subList.Contains(tag)) subList.Add(tag);
        }

        public bool IsTagPresentInSubList(string subListName, Tag tag)
        {
            if (!tagsMap.ContainsKey(subListName)) return false;

            var subList = tagsMap[subListName];

            if (!subList.Contains(tag)) return false;

            return true;
        }

        public void RemoveTag(Tag tag)
        {
            tags.RemoveAll(t => t.id == tag.id);
        }
        public void RemoveTag(int id)
        {
            tags.RemoveAll(t => t.id == id);
        }

        public Tag GetTag(int id)
        {
            return tags.Find(t => t.id == id);
        }

        public Tag GetTag(string name)
        {
            return tags.Find(t => t.name == name);
        }

        public bool ContainTag(int id)
        {
            return tags.Exists(t => t.id == id);
        }

        public bool ContainTag(string name)
        {
            return tags.Exists(t => t.name == name);
        }

        public List<Tag> GetAllTags()
        {
            return new List<Tag>(tags);
        }
    }

    public class TagDatabase
    {
        private Dictionary<string, TagCategory> tagDatabase = new();

        //Adding a new Category
        public void AddCategory(string categoryName)
        {
            if (!tagDatabase.ContainsKey(categoryName.ToLower()))
                tagDatabase.Add(categoryName.ToLower(), new TagCategory(categoryName.ToLower()));
        }

        public void AddTag(string categoryName, Tag tag)
        {
            if (!tagDatabase.ContainsKey(categoryName.ToLower()))
                AddCategory(categoryName);

            tagDatabase[categoryName].AddTag(tag);
        }

        public void AddCategory(TagCategory category)
        {
            if (!tagDatabase.ContainsKey(category.categoryName)) tagDatabase.Add(category.categoryName, category);
        }

        public List<Tag> GetTagsByCategory(string category)
        {
            if (tagDatabase.ContainsKey(category.ToLower()))
                return tagDatabase[category].GetAllTags();
            return new List<Tag>();
        }

        public Tag GetTagById(string category, int id)
        {
            if (tagDatabase.ContainsKey(category.ToLower()))
                return tagDatabase[category].GetTag(id);
            return null;
        }

        public Tag GetTagByString(string category, string name)
        {
            if (tagDatabase.ContainsKey(category.ToLower()))
                return tagDatabase[category.ToLower()].GetTag(name);
            return null;
        }

        public TagCategory GetCategory(string categoryName)
        {
            return tagDatabase.GetValueOrDefault(categoryName.ToLower());
        }
        public bool ContainTag(string category, int id)
        {
            return tagDatabase.ContainsKey(category.ToLower()) && tagDatabase[category].ContainTag(id);
        }

        public bool ContainTag(string category, string name)
        {
            return tagDatabase.ContainsKey(category.ToLower()) && tagDatabase[category].ContainTag(name);
        }
    }
}