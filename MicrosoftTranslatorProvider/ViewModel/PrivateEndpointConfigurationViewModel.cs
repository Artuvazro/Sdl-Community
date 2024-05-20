﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LanguageMappingProvider.Database.Interface;
using MicrosoftTranslatorProvider.Commands;
using MicrosoftTranslatorProvider.Interfaces;
using MicrosoftTranslatorProvider.Model;
using Sdl.LanguagePlatform.Core;

namespace MicrosoftTranslatorProvider.ViewModel
{
	public class PrivateEndpointConfigurationViewModel : BaseViewModel
    {
		readonly ILanguageMappingDatabase _languageMappingDatabase;
		readonly ITranslationOptions _translationOptions;
		readonly LanguagePair[] _languagePairs;

		string _loadingAction;

		string _headerKey;
		string _headerValue;
		string _parameterKey;
		string _parameterValue;

		public PrivateEndpointConfigurationViewModel(ITranslationOptions translationOptions, LanguagePair[] languagePairs, ILanguageMappingDatabase languageMappingDatabase)
		{
			_languagePairs = languagePairs;
			_translationOptions = translationOptions;
			_languageMappingDatabase = languageMappingDatabase;
			InitializeCommands();
			LoadPairMapping();
			LoadConfigurations();
		}

		public ObservableCollection<UrlMetadata> Headers { get; set; }

		public ObservableCollection<UrlMetadata> Parameters { get; set; }

		public ObservableCollection<PairModel> PairMappings { get; set; }

		public string HeaderKey
		{
			get => _headerKey;
			set
			{
				_headerKey = value;
				OnPropertyChanged();
			}
		}

		public string HeaderValue
		{
			get => _headerValue;
			set
			{
				_headerValue = value;
				OnPropertyChanged();
			}
		}

		public string ParameterKey
		{
			get => _parameterKey;
			set
			{
				_parameterKey = value;
				OnPropertyChanged();
			}
		}

		public string ParameterValue
		{
			get => _parameterValue;
			set
			{
				_parameterValue = value;
				OnPropertyChanged();
			}
		}

		public string LoadingAction
		{
			get => _loadingAction;
			set
			{
				_loadingAction = value;
				OnPropertyChanged();
			}
		}

		public ICommand AddHeaderCommand { get; set; }
		public ICommand AddParameterCommand { get; set; }
		public ICommand DeletePairCommand { get; set; }

		private void InitializeCommands()
		{
			AddHeaderCommand = new RelayCommand(AddHeader);
			AddParameterCommand = new RelayCommand(AddParameter);
			DeletePairCommand = new RelayCommand(DeletePair);
		}

		private void LoadConfigurations()
		{
			Headers = [];
			Parameters = [];
			if (_translationOptions.PrivateEndpoint is null
			 || _translationOptions.PrivateEndpoint.Parameters is null)
			{
				AddDefaultParemeters();
			}

			Headers = LoadUrlMetadataCollection(_translationOptions.PrivateEndpoint.Headers) ?? [];
			Parameters = LoadUrlMetadataCollection(_translationOptions.PrivateEndpoint.Parameters) ?? Parameters ?? [];
		}

		private void LoadPairMapping()
		{
			if (_translationOptions.PairMappings is null)
			{
				CreatePairMappings();
				return;
			}


			PairMappings = new(_translationOptions.PairModels.Select(pm => pm.Clone()));
		}

		private async void CreatePairMappings()
		{
			var originalPairMappings = PairMappings;
			PairMappings = [];
			LoadingAction = "Getting mapped languages...";
			await Task.Delay(0);
			var mappedLanguages = _languageMappingDatabase.GetMappedLanguages();
			foreach (var languagePair in _languagePairs)
			{
				var mappedLanguagePairs = mappedLanguages.Where(mappedLang => mappedLang.TradosCode.Equals(languagePair.SourceCultureName) || mappedLang.TradosCode.Equals(languagePair.TargetCultureName));
				var mappedSource = mappedLanguagePairs.FirstOrDefault(mappedLang => mappedLang.TradosCode.Equals(languagePair.SourceCultureName));
				var mappedTarget = mappedLanguagePairs.FirstOrDefault(mappedLang => mappedLang.TradosCode.Equals(languagePair.TargetCultureName));
				var displayName = $"{mappedSource.Name} ({mappedSource.Region}) - {mappedTarget?.Name} ({mappedTarget?.Region})";
				var sourceLanguageCode = mappedSource.LanguageCode;
				var targetLanguageCode = mappedTarget.LanguageCode;

				var currentModel = originalPairMappings?.FirstOrDefault(pair => pair.DisplayName.Equals(displayName));
				if (currentModel is not null
				 && sourceLanguageCode.Equals(currentModel.SourceLanguageCode)
				 && targetLanguageCode.Equals(currentModel.TargetLanguageCode))
				{
					PairMappings.Add(currentModel.Clone());
					continue;
				}

				var pairModel = new PairModel()
				{
					DisplayName = displayName,
					IsSupported = true,
					SourceLanguageCode = sourceLanguageCode,
					SourceLanguageName = mappedSource.Name,
					TargetLanguageCode = targetLanguageCode,
					TargetLanguageName = mappedTarget.Name,
					TradosLanguagePair = languagePair
				};

				PairMappings.Add(pairModel);
			}

			OnPropertyChanged(nameof(PairMappings));
			LoadingAction = string.Empty;
		}

		private ObservableCollection<UrlMetadata> LoadUrlMetadataCollection(IEnumerable<UrlMetadata> source)
		{
			if (source?.Any() != true)
			{
				return null;
			}

			return new ObservableCollection<UrlMetadata>(source.Select(parameter => new UrlMetadata()
			{
				Key = parameter.Key,
				Value = parameter.Value,
				IsReadOnly = parameter.IsReadOnly
			}));
		}

		private void AddDefaultParemeters()
		{
			Parameters =
			[
				new UrlMetadata()
				{
					Key = "from",
					Value = "sourceLanguage",
					IsReadOnly = true
				},
				new UrlMetadata()
				{
					Key = "to",
					Value = "targetLanguage",
					IsReadOnly = true
				},
			];
		}

		private void AddHeader(object parameter)
		{
			var newHeader = new UrlMetadata()
			{
				Key = HeaderKey,
				Value = HeaderValue,
			};

			AddItem(Headers, newHeader);
			HeaderKey = string.Empty;
			HeaderValue = string.Empty;
		}

		private void AddParameter(object parameter)
		{
			var newParameter = new UrlMetadata()
			{
				Key = ParameterKey,
				Value = ParameterValue,
			};

			AddItem(Parameters, newParameter);
			ParameterKey = string.Empty;
			ParameterValue = string.Empty;
		}

		private void AddItem(ObservableCollection<UrlMetadata> items, UrlMetadata item)
		{
			items.Add(item);
		}

		private void DeletePair(object parameter)
		{
			if (parameter is not UrlMetadata pair)
			{
				return;
			}

			Headers.Remove(pair);
			Parameters.Remove(pair);
		}
	}
}