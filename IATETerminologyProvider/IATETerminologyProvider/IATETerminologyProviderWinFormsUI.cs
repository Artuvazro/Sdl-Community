﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IATETerminologyProvider.Service;
using IATETerminologyProvider.Ui;
using Sdl.Terminology.TerminologyProvider.Core;

namespace IATETerminologyProvider
{
	[TerminologyProviderWinFormsUI]
	public class IATETerminologyProviderWinFormsUI : ITerminologyProviderWinFormsUI
	{
		public string TypeName => PluginResources.IATETerminologyProviderName;
		public string TypeDescription => PluginResources.IATETerminologyProviderDescription;

		public bool SupportsEditing => true;

		public ITerminologyProvider[] Browse(IWin32Window owner, ITerminologyProviderCredentialStore credentialStore)
		{
			var result = new List<ITerminologyProvider>();

			var settingsDialog = new Settings();
			var dialogResult = settingsDialog.ShowDialog();
			if (dialogResult == DialogResult.OK ||
				dialogResult == DialogResult.Yes)
			{
				var providerSettings = settingsDialog.GetSettings();
				var persistenceService = new PersistenceService();
				persistenceService.AddSettings(providerSettings);
				var termSearchService = new TermSearchService(providerSettings);
				var IATETerminologyProvider = new IATETerminologyProvider(providerSettings);

				result.Add(IATETerminologyProvider);
			}
			return result.ToArray();
		}

		public bool Edit(IWin32Window owner, ITerminologyProvider terminologyProvider)
		{
			return true;
		}

		public TerminologyProviderDisplayInfo GetDisplayInfo(Uri terminologyProviderUri)
		{
			return new TerminologyProviderDisplayInfo
			{
				Name = "IATE Terminology Provider",
				TooltipText = "IATE Terminology Provider"
			};
		}

		public bool SupportsTerminologyProviderUri(Uri terminologyProviderUri)
		{
			return terminologyProviderUri.Scheme == "iateglossary";
		}
	}
}