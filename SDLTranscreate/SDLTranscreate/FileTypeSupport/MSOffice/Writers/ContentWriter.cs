﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sdl.Community.Transcreate.FileTypeSupport.MSOffice.Model;
using Sdl.Community.Transcreate.FileTypeSupport.MSOffice.Readers;
using Sdl.Community.Transcreate.Model;
using Sdl.Core.Globalization;
using Sdl.FileTypeSupport.Framework.BilingualApi;
using Sdl.FileTypeSupport.Framework.NativeApi;

namespace Sdl.Community.Transcreate.FileTypeSupport.MSOffice.Writers
{
	internal class ContentWriter : AbstractBilingualContentProcessor
	{
		private readonly GeneratorSettings _convertSettings;
		private readonly string _updatedFilePath;
		private readonly List<AnalysisBand> _analysisBands;
		private IDocumentProperties _documentProperties;
		private Dictionary<string, UpdatedSegmentContent> _updatedSegments;

		public ContentWriter(GeneratorSettings convertSettings, List<AnalysisBand> analysisBands, string updatedFilePath)
		{
			_convertSettings = convertSettings;
			_analysisBands = analysisBands;
			_updatedFilePath = updatedFilePath;

			ConfirmationStatistics = new ConfirmationStatistics();
			TranslationOriginStatistics = new TranslationOriginStatistics();
		}

		public ConfirmationStatistics ConfirmationStatistics { get; }

		public TranslationOriginStatistics TranslationOriginStatistics { get; }

		public CultureInfo SourceLanguage { get; private set; }

		public CultureInfo TargetLanguage { get; private set; }

		public override void Initialize(IDocumentProperties documentInfo)
		{
			_documentProperties = documentInfo;

			SourceLanguage = documentInfo.SourceLanguage.CultureInfo;
			TargetLanguage = documentInfo.TargetLanguage?.CultureInfo ?? SourceLanguage;

			base.Initialize(documentInfo);
		}

		public override void SetFileProperties(IFileProperties fileInfo)
		{
			base.SetFileProperties(fileInfo);
			//Read the updated DOCX file and collect all the segments
			var reader = new WordReader(_convertSettings);
			_updatedSegments = reader.ReadFile(_updatedFilePath);

			base.SetFileProperties(fileInfo);
		}

		public override void ProcessParagraphUnit(IParagraphUnit paragraphUnit)
		{
			if (paragraphUnit.IsStructure)
			{
				base.ProcessParagraphUnit(paragraphUnit);
				return;
			}


			foreach (var segmentPair in paragraphUnit.SegmentPairs)
			{
				if (!segmentPair.Target.AllSubItems.Any())
				{
					//original segment was empty, currently not supported
					continue;
				}

				var targetSegment = segmentPair.Target;

				//capture if segment contains tracked changes
				var hasTrackedChanges = false;

				var segmentIdentifier = string.Empty;
				if (SegmentExists(paragraphUnit.Properties.ParagraphUnitId.Id, segmentPair.Properties.Id.Id, ref segmentIdentifier))
				{
					try
					{
						//store original segment
						var originalSegment = (ISegment)targetSegment.Clone();

						//remove old content
						targetSegment.Clear();

						//we need to insert into proper node in the tree, it means to store information about location in the tree
						var vector = new List<int>();
						var lockedContentId = 0;
						foreach (var item in _updatedSegments[segmentIdentifier].Tokens)
						{
							switch (item.Type)
							{
								case Token.TokenType.TagOpen:
									var tagPairOpen = (IAbstractMarkupDataContainer)GetElement(GetTagID(item.Content), originalSegment, segmentPair.Source, item.Type);
									tagPairOpen.Clear();
									InsertItemOnLocation(vector, ref targetSegment, (IAbstractMarkupData)tagPairOpen);
									vector.Add(((ITagPair)tagPairOpen).IndexInParent);
									break;
								case Token.TokenType.TagClose:
									vector.RemoveAt(vector.Count - 1);
									break;
								case Token.TokenType.TagPlaceholder:
									InsertItemOnLocation(vector, ref targetSegment,
										GetElement(GetTagID(item.Content), originalSegment, segmentPair.Source, item.Type));
									break;
								case Token.TokenType.Text:
									InsertItemOnLocation(vector, ref targetSegment, ItemFactory.CreateText(
										PropertiesFactory.CreateTextProperties(item.Content)));
									break;
								case Token.TokenType.LockedContent:
									InsertItemOnLocation(vector, ref targetSegment, GetElement(lockedContentId.ToString(), originalSegment, segmentPair.Source, item.Type));
									lockedContentId++;
									break;
								case Token.TokenType.CommentStart:
									var commentContainer = GetComment(item);
									InsertItemOnLocation(vector, ref targetSegment, (IAbstractMarkupData)commentContainer);
									vector.Add(((ICommentMarker)commentContainer).IndexInParent);
									break;
								case Token.TokenType.CommentEnd:
									if (vector.Count > 0)
									{
										vector.RemoveAt(vector.Count - 1);
									}
									break;
								case Token.TokenType.RevisionMarker:
									hasTrackedChanges = true;
									if (item.RevisionType == Token.RevisionMarkerType.InsertStart)
									{
										var insertContainer = GetRevisionMarker(item, RevisionType.Insert);
										InsertItemOnLocation(vector, ref targetSegment, (IAbstractMarkupData)insertContainer);
										vector.Add(((IRevisionMarker)insertContainer).IndexInParent);
									}
									else if (item.RevisionType == Token.RevisionMarkerType.DeleteStart)
									{
										var insertContainer = GetRevisionMarker(item, RevisionType.Delete);
										InsertItemOnLocation(vector, ref targetSegment, (IAbstractMarkupData)insertContainer);
										vector.Add(((IRevisionMarker)insertContainer).IndexInParent);
									}
									else
									{
										if (vector.Count > 0)
										{
											vector.RemoveAt(vector.Count - 1);
										}
									}
									break;
							}
						}
					}
					catch (Exception ex)
					{
						throw new Exception("Problem when merging content of segment " + segmentPair.Properties.Id.Id, ex);
					}

					//update segment status
					segmentPair.Properties.ConfirmationLevel = UpdateSegmentStatus(hasTrackedChanges, segmentPair.Properties.ConfirmationLevel);
				}
				else
				{
					//update segment status
					segmentPair.Properties.ConfirmationLevel = UpdateSegmentStatus(hasTrackedChanges, segmentPair.Properties.ConfirmationLevel);
				}
			}

			base.ProcessParagraphUnit(paragraphUnit);
		}

		/// <summary>
		/// Need to find out the segment identifier, there is a possibility that the old files 
		/// are processed and the paragraph unit ID is not entered
		/// </summary>
		/// <param name="paragrahpUnitId"></param>
		/// <param name="segmentId"></param>
		/// <param name="segmentIdentifier"></param>
		/// <returns></returns>
		private bool SegmentExists(string paragrahpUnitId, string segmentId, ref string segmentIdentifier)
		{
			if (_updatedSegments.ContainsKey(paragrahpUnitId + "_" + segmentId))
			{
				segmentIdentifier = paragrahpUnitId + "_" + segmentId;
				return true;
			}
			if (_updatedSegments.ContainsKey("_" + segmentId))
			{
				segmentIdentifier = "_" + segmentId;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns the segment confirmation status - based on setting
		/// </summary>
		/// <param name="segmentHasChanges"></param>
		/// <param name="originalStatus"></param>
		/// <returns></returns>
		private ConfirmationLevel UpdateSegmentStatus(bool segmentHasChanges, ConfirmationLevel originalStatus)
		{
			//update segments with tracked changes
			if (segmentHasChanges && _convertSettings.ImportUpdateSegmentMode == GeneratorSettings.UpdateSegmentMode.TrackedOnly &&
				_convertSettings.UpdateSegmentStatusTracked)
			{
				return _convertSettings.NewSegmentStatusTrackedChanges;
			}

			//Update all segments - distinguish between segments with changes and without 
			if (_convertSettings.ImportUpdateSegmentMode == GeneratorSettings.UpdateSegmentMode.All)
			{
				if (_convertSettings.UpdateSegmentStatusTracked && segmentHasChanges)
				{
					return _convertSettings.NewSegmentStatusTrackedChanges;
				}
				if (_convertSettings.UpdateSegmentStatusNoTracked && !segmentHasChanges)
				{
					return _convertSettings.NewSegmentStatusAll;
				}
			}

			return originalStatus;
		}

		/// <summary>
		/// Create IRevisionMarker from specified Token
		/// </summary>
		/// <param name="item"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		private IAbstractMarkupDataContainer GetRevisionMarker(Token item, RevisionType type)
		{
			var revisionProperties = ItemFactory.CreateRevisionProperties(type);
			revisionProperties.Author = item.Author;
			revisionProperties.Date = item.Date;
			var revision = ItemFactory.CreateRevision(revisionProperties);
			return revision;
		}

		/// <summary>
		/// Create ICommentMarker from specified Token
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private IAbstractMarkupDataContainer GetComment(Token item)
		{
			var commentProperties = PropertiesFactory.CreateCommentProperties();
			var comment = PropertiesFactory.CreateComment(item.Content, item.Author, Severity.Low);
			comment.Date = item.Date;
			commentProperties.Add(comment);
			var commentMarker = ItemFactory.CreateCommentMarker(commentProperties);
			return commentMarker;
		}

		/// <summary>
		/// Insert item to the proper container
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="segment"></param>
		/// <param name="abstractItem"></param>
		private void InsertItemOnLocation(IEnumerable<int> vector, ref ISegment segment, IAbstractMarkupData abstractItem)
		{
			IAbstractMarkupDataContainer currentContainer = segment;

			foreach (var index in vector)
			{
				currentContainer = (IAbstractMarkupDataContainer)currentContainer[index];
			}

			currentContainer.Add(abstractItem);
		}

		/// <summary>
		/// Get the MarkupData ID
		/// </summary>
		/// <param name="tagContent"></param>
		/// <returns></returns>
		private string GetTagID(string tagContent)
		{
			return tagContent.Replace("<", "").Replace(">", "").Replace("/", "");
		}

		/// <summary>
		/// Get the required MarkupData from the original segment, if the tag doesn't exist in original target, source segment will be searched as well.
		/// </summary>
		/// <param name="tagId"></param>
		/// <param name="originalTargetSegment"></param>
		/// <param name="sourceSegment"></param>
		/// <param name="tokenType"></param>
		/// <returns></returns>
		private IAbstractMarkupData GetElement(string tagId, ISegment originalTargetSegment, ISegment sourceSegment, Token.TokenType tokenType)
		{
			var extractor = new Visitors.ElementExtractor();
			extractor.GetTag(tagId, originalTargetSegment, tokenType);
			if (extractor.FoundElement != null)
			{
				return (IAbstractMarkupData)extractor.FoundElement.Clone();
			}

			//tag not found in original target, try to search in source
			extractor.GetTag(tagId, sourceSegment, tokenType);
			if (extractor.FoundElement != null)
			{
				return (IAbstractMarkupData)extractor.FoundElement.Clone();
			}

			if (tokenType == Token.TokenType.TagOpen || tokenType == Token.TokenType.TagClose || tokenType == Token.TokenType.TagPlaceholder)
			{
				throw new Exception("Tags in segment ID " + originalTargetSegment.Properties.Id.Id + " are corrupted!");
			}
			if (tokenType == Token.TokenType.LockedContent)
			{
				throw new Exception("Locked contents in segment ID " + originalTargetSegment.Properties.Id.Id + " are corrupted!");
			}

			throw new Exception("Problem when reading segment #" + originalTargetSegment.Properties.Id.Id);
		}
	}
}
