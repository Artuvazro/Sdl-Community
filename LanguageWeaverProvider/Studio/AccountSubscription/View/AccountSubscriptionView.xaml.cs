﻿using System.Windows.Controls;
using Sdl.Desktop.IntegrationApi.Interfaces;

namespace LanguageWeaverProvider.Studio.AccountSubscription.View
{
	/// <summary>
	/// Interaction logic for AccountSubscriptionView.xaml
	/// </summary>
	public partial class AccountSubscriptionView : UserControl, IUIControl
	{
		public AccountSubscriptionView()
		{
			InitializeComponent();
		}

		public void Dispose() { }
	}
}