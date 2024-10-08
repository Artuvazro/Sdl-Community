﻿using System.Collections.ObjectModel;

namespace InterpretBank.SettingsService.Model
{
    public class GlossaryModel : InterpretBank.Model.NotifyChangeModel
    {
        private ObservableCollection<object> _languages = new();
		private ObservableCollection<TagModel> _tags = new();
		public string GlossaryName { get; set; }
		public string SubGlossaryName { get; set; }
		public int Id { get; set; }

		public ObservableCollection<object> Languages
		{
			get => _languages;
			set => SetField(ref _languages, value);
		}

		//public ObservableCollection<TagModel> Tags
		//{
		//	get => _tags;
		//	set => SetField(ref _tags, value);
		//}

        public override string ToString() => $"{GlossaryName}{SubGlossaryName}";
    }
}