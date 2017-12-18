﻿using System;
using System.ComponentModel;

namespace Sdl.Community.BackupService.Helpers
{
	public static class Enums
	{
		public enum TimeTypes
		{		
			[Description("minutes")]
			Minutes = 0,
			[Description("hours")]
			Hours = 1
		}

		public static string GetDescription(this Enum value)
		{
			Type type = value.GetType();
			string name = Enum.GetName(type, value);
			if (name != null)
			{
				System.Reflection.FieldInfo field = type.GetField(name);
				if (field != null)
				{
					DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

					if (attr != null)
					{
						return attr.Description;
					}
				}
			}
			return null;
		}
	}
}