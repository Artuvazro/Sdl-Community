﻿namespace Sdl.Community.MTEdge.Provider.Helpers
{
	public static class Constants
	{
		// Logging messages
		public readonly static string BuildUri = "BuildUri method";
		public readonly static string AuthenticateCredentials = "AuthenticateCredentials method";
		public readonly static string VerifyBasicAPIToken = "VerifyBasicAPIToken method";
		public readonly static string Translation = "GetTranslation method";
		public readonly static string LanguagePairs = "GetLanguagePairs method";
		public readonly static string InaccessibleLangPairs = "Unable to get the language pairs:";
		public readonly static string MTEdgeServerContact = "ContactMTEdgeServer method:";
		public readonly static string MtEdgeServerContactExResult = "Exception thrown when translating.Hresult is:";
		public readonly static string InvalidCredentials = "Invalid credentials received.";
		public readonly static string BadRequest = "Bad request received:";
		public readonly static string StatusCode = "status code resulting in failure of segment";
		public readonly static string AuthToken = "GetAuthToken method";
		public readonly static string MTEdgeApiVersion = "SetMtEdgeApiVersion method";
		public readonly static string TranslateAggregateException = "TranslateAggregateException method";
		public readonly static string NoProviderSetup = "The provider cannot be setup because the language flavor was not received from the Weaver Edge server.";

		public readonly static string NoDictionary = "No dictionary";
	}
}