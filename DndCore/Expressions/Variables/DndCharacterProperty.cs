﻿using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using CodingSeb.ExpressionEvaluator;

namespace DndCore
{
	public class DndCharacterProperty : DndVariable
	{
		public delegate void AskValueEventHandler(object sender, AskValueEventArgs ea);
		public static event AskValueEventHandler AskingValue;
		private static AskValueEventArgs askValueEventArgs;

		List<string> propertyNames = null;
		List<string> fieldNames = null;
		
		void GetPropertyNames()
		{
			if (propertyNames != null)
				return;
			propertyNames = new List<string>();
			fieldNames = new List<string>();
			PropertyInfo[] properties = typeof(Character).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (PropertyInfo propertyInfo in properties)
			{
				propertyNames.Add(propertyInfo.Name);
			}

			FieldInfo[] fields = typeof(Character).GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (FieldInfo fieldInfo in fields)
			{
				fieldNames.Add(fieldInfo.Name);
			}
		}

		public override bool Handles(string tokenName, Creature player, CastedSpell castedSpell)
		{
			if (player == null)
				return false;

			GetPropertyNames();
			if (propertyNames.IndexOf(tokenName) >= 0 | fieldNames.IndexOf(tokenName) >= 0)
				return true;

			if (KnownQualifiers.StartsWithKnownQualifier(tokenName))
				return false;

			return player.HoldsState(tokenName) || tokenName.StartsWith("_"); // ;
		}

		object AskValue(Creature player, string caption, object currentValue, string memberName, string memberTypeName)
		{
			if (askValueEventArgs == null)
				askValueEventArgs = new AskValueEventArgs(player, caption, memberName, memberTypeName, currentValue);
			else
			{
				askValueEventArgs.Player = player;
				askValueEventArgs.Caption = caption;
				askValueEventArgs.MemberName = memberName;
				askValueEventArgs.MemberTypeName = memberTypeName;
				askValueEventArgs.Value = currentValue;
			}

			AskingValue?.Invoke(this, askValueEventArgs);
			return askValueEventArgs.Value;
		}

		void CheckValue(Creature player, MemberInfo member, ref object value)
		{
			AskAttribute askAttr = member?.Get<AskAttribute>();
			if (askAttr == null)
				return;

			string askCaption = askAttr.Caption;
			string memberType = null;
			if (member is PropertyInfo propInfo)
				memberType = propInfo.PropertyType.Name;
			else if (member is FieldInfo fieldInfo)
				memberType = fieldInfo.FieldType.Name;

			value = AskValue(player, askCaption, value, member.Name, memberType);
		}

		public override object GetValue(string variableName, ExpressionEvaluator evaluator, Creature player)
		{
			if (fieldNames.IndexOf(variableName) >= 0)
			{
				FieldInfo field = typeof(Character).GetField(variableName);

				object value = field?.GetValue(player);
				CheckValue(player, field, ref value);
				return value;
			}

			if (propertyNames.IndexOf(variableName) >= 0)
			{
				PropertyInfo property = typeof(Character).GetProperty(variableName);

				object value = property?.GetValue(player);
				CheckValue(player, property, ref value);
				return value;
			}

			return player.GetState(variableName);
		}

		public override List<PropertyCompletionInfo> GetCompletionInfo()
		{
			List<PropertyCompletionInfo> result = new List<PropertyCompletionInfo>();
			GetPropertyNames();
			foreach (string property in propertyNames)
			{
				string propertyDescription = $"Active Character Property: {property}";
				PropertyInfo propertyInfo = typeof(Character).GetProperty(property);
				if (propertyInfo != null)
				{
					DescriptionAttribute descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
					if (descriptionAttribute != null)
						propertyDescription = descriptionAttribute.Description;
				}
				TypeHelper.GetTypeDetails(propertyInfo.PropertyType, out string enumTypeName, out ExpressionType expressionType);
				result.Add(new PropertyCompletionInfo() { Name = property, Description = propertyDescription, EnumTypeName = enumTypeName, Type = expressionType });
			}
			foreach (string fieldName in fieldNames)
			{
				string fieldDescription = $"Active Character Field: {fieldName}";
				FieldInfo fieldInfo = typeof(Character).GetField(fieldName);
				if (fieldInfo != null)
				{
					DescriptionAttribute descriptionAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
					if (descriptionAttribute != null)
						fieldDescription = descriptionAttribute.Description;
				}
				TypeHelper.GetTypeDetails(fieldInfo.FieldType, out string enumTypeName, out ExpressionType expressionType);
				result.Add(new PropertyCompletionInfo() { Name = fieldName, Description = fieldDescription, EnumTypeName = enumTypeName, Type = expressionType });
			}
			return result;
			
		}
	}
}

