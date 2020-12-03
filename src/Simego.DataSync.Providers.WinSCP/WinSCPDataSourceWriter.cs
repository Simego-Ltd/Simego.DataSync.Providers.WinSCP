using Simego.DataSync.Engine;
using Simego.DataSync.Interfaces;
using System;
using System.Collections.Generic;
using WinSCP;

namespace Simego.DataSync.Providers.WinSCP
{
    public class WinSCPDataSourceWriter : DataWriterProviderBase
    {
        private WinSCPDatasourceReader DataSourceReader { get; set; }
        private DataSchemaMapping Mapping { get; set; }
        private IDataSourceReader SourceReader => DataCompare.SourceObject;
        private Session Session { get; set; }
        public override void AddItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        var itemInvariant = new DataCompareItemInvariant(item);
                        //Get the Target Item Data
                        Dictionary<string, object> targetItem = AddItemToDictionary(Mapping, itemInvariant);

                        string fileName = DataSchemaTypeConverter.ConvertTo<string>(targetItem["FullFileName"]) ?? DataSchemaTypeConverter.ConvertTo<string>(targetItem["FileName"]);

                        fileName = Utility.CombineWebPath(DataSourceReader.Path, Utility.EnsureWebPath(fileName));
                        
                        Automation?.BeforeAddItem(this, itemInvariant, fileName);

                        if (itemInvariant.Sync)
                        {
                            string tmpFile = System.IO.Path.Combine(Utility.GetTempPath(), $"{Guid.NewGuid()}.tmp");
                            
                            try
                            {
                                System.IO.File.WriteAllBytes(tmpFile, SourceReader.GetBlobData(itemInvariant.ToDataCompareItem(), 0));
                                if (targetItem.TryGetValue("DateModified", out object value))
                                {
                                    System.IO.File.SetLastWriteTimeUtc(tmpFile, DataSchemaTypeConverter.ConvertTo<DateTime>(value).ToUniversalTime());
                                }

                                CreateDirectory(Utility.EnsureWebPath(System.IO.Path.GetDirectoryName(fileName)));
                                
                                Session.PutFiles(tmpFile, fileName, false, new TransferOptions { PreserveTimestamp = true, TransferMode = TransferMode.Binary }).Check();
                            }
                            catch(Exception e)
                            {
                                Automation?.ErrorItem(this, itemInvariant, fileName, e);
                                throw;
                            }
                            finally
                            {
                                if (System.IO.File.Exists(tmpFile))
                                    System.IO.File.Delete(tmpFile);

                            }

                            Automation?.AfterAddItem(this, itemInvariant, null);
                        }
                        
                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows

                    }
                    catch (SystemException e)
                    {
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void UpdateItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        var itemInvariant = new DataCompareItemInvariant(item);
                        var filename = itemInvariant.GetTargetIdentifier<string>();

                        Automation?.BeforeUpdateItem(this, itemInvariant, filename);

                        if (itemInvariant.Sync)
                        {
                            #region Update Item

                            //Get the Target Item Data
                            Dictionary<string, object> targetItem = UpdateItemToDictionary(Mapping, itemInvariant);

                            string tmpFile = System.IO.Path.Combine(Utility.GetTempPath(), $"{Guid.NewGuid()}.tmp");
                                                        
                            try
                            {
                                System.IO.File.WriteAllBytes(tmpFile, SourceReader.GetBlobData(itemInvariant.ToDataCompareItem(), 0));
                                if (targetItem.TryGetValue("DateModified", out object value))
                                {
                                    System.IO.File.SetLastWriteTimeUtc(tmpFile, DataSchemaTypeConverter.ConvertTo<DateTime>(value).ToUniversalTime());
                                }

                                Session.PutFiles(tmpFile, filename, false, new TransferOptions { OverwriteMode = OverwriteMode.Overwrite, PreserveTimestamp = true, TransferMode = TransferMode.Binary }).Check();

                            }
                            catch (Exception e)
                            {
                                Automation?.ErrorItem(this, itemInvariant, filename, e);
                                throw;
                            }
                            finally
                            {
                                if (System.IO.File.Exists(tmpFile))
                                    System.IO.File.Delete(tmpFile);
                            }

                            Automation?.AfterUpdateItem(this, itemInvariant, filename);                            
                            #endregion
                        }

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows
                    }
                    catch (SystemException e)
                    {
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void DeleteItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        var itemInvariant = new DataCompareItemInvariant(item);
                        var filename = itemInvariant.GetTargetIdentifier<string>();

                        Automation?.BeforeDeleteItem(this, itemInvariant, filename);

                        if (itemInvariant.Sync)
                        {
                            try
                            {
                                Session.RemoveFile(filename);
                                Automation?.AfterDeleteItem(this, itemInvariant, filename);
                            }
                            catch (Exception e)
                            {
                                Automation?.ErrorItem(this, itemInvariant, filename, e);
                                throw;
                            }
                        }

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows
                    }
                    catch (SystemException e)
                    {
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void Execute(List<DataCompareItem> addItems, List<DataCompareItem> updateItems, List<DataCompareItem> deleteItems, IDataSourceReader reader, IDataSynchronizationStatus status)
        {
            DataSourceReader = reader as WinSCPDatasourceReader;

            if (DataSourceReader != null)
            {
                using (Session = DataSourceReader.GetSession())
                {
                    Mapping = new DataSchemaMapping(SchemaMap, DataCompare);

                    //Process the Changed Items
                    if (addItems != null && status.ContinueProcessing) AddItems(addItems, status);
                    if (updateItems != null && status.ContinueProcessing) UpdateItems(updateItems, status);
                    if (deleteItems != null && status.ContinueProcessing) DeleteItems(deleteItems, status);
                }
            }
        }

        private static void HandleError(IDataSynchronizationStatus status, Exception e)
        {
            if (!status.FailOnError)
            {
                status.LogMessage(e.Message);
            }
            if (status.FailOnError)
            {
                throw e;
            }
        }

        private void CreateDirectory(string path)
        {
            if (Session.FileExists(path)) return;

            var parts = Utility.StripStartSlash(path).Split('/');

            // Find the shortest path that doesn't exist.
            int i = 0;
            for (i=parts.Length-1; i>=0; i--)
            {
                var p = "/" + string.Join("/", parts, 0, i);
                if (Session.FileExists(p))
                    break;
            }

            // Create folders for the path
            for(int j=i+1; j<=parts.Length; j++)
            {
                var p = "/" + string.Join("/", parts, 0, j);
                Session.CreateDirectory(p);
            }
        }
    }
}
