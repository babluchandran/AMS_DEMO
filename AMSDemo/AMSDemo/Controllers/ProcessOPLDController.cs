﻿using Microsoft.AspNetCore.Mvc;
using System.IO;
using AMSDemo.Models;
using AMSDemo.Utility;
using AMSDemo.DatabaseOps;
using System.Threading;
using System;
using Microsoft.Extensions.Configuration;

namespace AMSDemo.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProcessOPLDController : ControllerBase
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //[HttpPost]
        //public IActionResult ProcessOPLDNPushTOMQ1()
        //{
        //    try
        //    {
        //        log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD file processing started.");

        //        //Process OPLD data             
        //        var opldProcString = Request.Headers["opldProcString"];

        //        OPLD opldObject = Newtonsoft.Json.JsonConvert.DeserializeObject<OPLD>(opldProcString);

        //        log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD file processing completed.");

        //        //Push OPLD in to Active MQ1
        //        if (!string.IsNullOrEmpty(opldObject.TrackingNumber))
        //        {
        //            CommonUtility<OPLD>.PushToActiveMQ(opldObject, 1);

        //            log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD message pushed to MQ1.");
        //        }
        //        else
        //        {
        //            log.Warn(DateTime.Now.ToString() + " AMS-POC: Tracking number not found in OPLD message.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error(DateTime.Now.ToString() + " AMS-POC: " + Convert.ToString(ex.Message));
        //        return new JsonResult(new { Result = System.Net.HttpStatusCode.InternalServerError });
        //    }

        //    return Ok(new { Result = "Success" });
        //}
               
        [HttpPost]
        public IActionResult ProcessOPLDNPushTOMQ1(OPLD opldObject)
        {
            try
            {
                //log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD file processing started.");

                //Process OPLD data             
                //var opldProcString = Request.Headers["opldProcString"];

                //OPLD opldObject = Newtonsoft.Json.JsonConvert.DeserializeObject<OPLD>(opldProcString);

                //log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD file processing completed.");

                //Push OPLD in to Active MQ1
                if (!string.IsNullOrEmpty(opldObject.TrackingNumber))
                {
                    CommonUtility<OPLD>.PushToActiveMQ(opldObject, 1);

                    log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD message pushed to MQ1.");
                }
                else
                {
                    log.Warn(DateTime.Now.ToString() + " AMS-POC: Tracking number not found in OPLD message.");
                }
            }
            catch (Exception ex)
            {
                log.Error(DateTime.Now.ToString() + " AMS-POC: " + Convert.ToString(ex.Message));
                return new JsonResult(new { Result = System.Net.HttpStatusCode.InternalServerError });
            }

            return Ok(new { Result = "Success" });
        }
        
        public void OPLDFileWatcher()
        {
            try
            {
                var opldFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "OPLDFiles");

                var files = Directory.GetFiles(opldFolderPath);

                if (files.Length > 0)
                {
                    foreach (string fileName in files)
                    {
                        log.Info(DateTime.Now.ToString() + " AMS-POC-MicroServiceProcessOPLDNDIALSFiles: OPLD file Read in Progress.");

                        string opldString = System.IO.File.ReadAllText(Path.Combine(opldFolderPath, fileName));

                        //Process OPLD data
                        var opldObject = OPLDUtility.ProcessOPLD(opldString);

                        //Check is File already Processed
                        SakilaContext context = new SakilaContext("server=techm.cooavdyjxzoz.us-east-1.rds.amazonaws.com;port=3306;database=ams;user=root;password=Password123");                        

                        bool trackResult = context.CheckIsTrackingNumberAlreadyExists(opldObject.TrackingNumber);
                        if (trackResult)
                        {
                            log.Info(DateTime.Now.ToString() + " AMS-POC-MicroServiceProcessOPLDNDIALSFiles: OPLD file Read already processed.");
                            continue;
                        }

                        log.Info(DateTime.Now.ToString() + " AMS-POC-MicroServiceProcessOPLDNDIALSFiles: OPLD file Processed.");

                        //Push OPLD to Queue
                        MicroServiceProcessOPLDFile(opldObject);

                        log.Info(DateTime.Now.ToString() + " AMS-POC-MicroServiceProcessOPLDNDIALSFiles: OPLD Message Pushed to MQ.");
                        
                        var archiveFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Archive");
                        
                        DirectoryInfo directoryInfo =  Directory.CreateDirectory(archiveFolderPath);

                        if (!System.IO.File.Exists(archiveFolderPath + fileName.Substring(fileName.LastIndexOf("\\"))))
                        {
                            System.IO.File.Move(fileName, archiveFolderPath + fileName.Substring(fileName.LastIndexOf("\\")));
                        }
                        else
                        {
                            System.IO.File.Delete(fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(DateTime.Now.ToString() + " AMS-MicroServiceProcessOPLDNDIALSFiles: " + Convert.ToString(ex.Message));
            }
        }

//        public void DIALSFileWatcher()
  //      {
 //           try
//            {
//                var dialsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "DIALSFiles");

 //               var files = Directory.GetFiles(dialsFolderPath);
public void DIALSFileWatcher()
        {
            try
            {
                var dialsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "DIALSFiles");

                var files = Directory.GetFiles(dialsFolderPath);

                if (files.Length > 0)
                {
                    foreach (string fileName in files)
                    {
                        log.Info(DateTime.Now.ToString() + " AMS-POC-MicroServiceProcessOPLDNDIALSFiles: DIALS file Read in Progress.");
                        FileStream fileStream = new FileStream(Path.Combine(dialsFolderPath, fileName), FileMode.Open);
                        using (BufferedStream bufferedStream = new BufferedStream(fileStream))
                        {
                            using (StreamReader streamReader = new StreamReader(bufferedStream))
                            {
                                while (!streamReader.EndOfStream)
                                {
                                    string dialsString = streamReader.ReadLine();

                                    if (!string.IsNullOrEmpty(dialsString.Trim()) && dialsString != "\u001a")
                                    {
                                        //Process DIALS data
                                        var dialsObject = DIALSUtility.ProcessDIALSData(dialsString);

                                        SakilaContext context = new SakilaContext("server=techm.cooavdyjxzoz.us-east-1.rds.amazonaws.com;port=3306;database=ams;user=root;password=Password123");
                                        DIALS dials = context.GetMatchingDialsID(dialsObject.TrackingNumber);

                                        //Store in to DB
                                        if (!string.IsNullOrEmpty(dialsObject.TrackingNumber) && (dialsObject.TrackingNumber != dials.TrackingNumber))
                                        {
                                            MicroServiceProcessDIALSFile(dialsObject);
                                        }
                                        else
                                        {
                                            log.Warn(DateTime.Now.ToString() + " AMS-MicroServiceProcessOPLDNDIALSFiles: Tracking number not found in DIALS data.");
                                        }
                                    }
                                }
                            }
                        }

                        fileStream.Close();
                        var archiveFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Archive");

                        DirectoryInfo directoryInfo = Directory.CreateDirectory(archiveFolderPath);

                        if (!System.IO.File.Exists(archiveFolderPath + fileName.Substring(fileName.LastIndexOf("\\"))))
                        {
                            System.IO.File.Move(fileName, archiveFolderPath + fileName.Substring(fileName.LastIndexOf("\\")));
                        }
                        else
                        {
                            System.IO.File.Delete(fileName);
                        }
                    }
                }

                log.Info(DateTime.Now.ToString() + " AMS-POC-MicroServiceProcessOPLDNDIALSFiles: DIALS file processed and data inserted into DB.");
            }
            catch (Exception ex)
            {
                log.Error(DateTime.Now.ToString() + " AMS-MicroServiceProcessOPLDNDIALSFiles: " + Convert.ToString(ex.Message));
            }
            
        }


                
                
                    
                    
                    
                       
           
                      
                           
                            
                           
                                
                                

                                    
                                        //Process DIALS data
                                   
                                        //Store in t

        //MicroService 1
        //[HttpPost]
        // public IActionResult ProcessOPLDNPushTOMQ1()
        // {
        //     try
        //     {
        //         log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD file processing started.");
        //         string opldFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "OPLDFiles");

        //         if (Directory.Exists(opldFolderPath))
        //         {
        //             var files = Directory.GetFiles(opldFolderPath);

        //             if (files.Length > 0)
        //             {
        //                 foreach (string fileName in files)
        //                 {
        //                     log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD file processing in progress.");
        //                     string opldString = System.IO.File.ReadAllText(Path.Combine(opldFolderPath, fileName));

        //                     //Process OPLD data
        //                     var opldObject = OPLDUtility.ProcessOPLD(opldString);

        //                     log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD file processing completed.");

        //                     //Push OPLD in to Active MQ1
        //                     if (!string.IsNullOrEmpty(opldObject.TrackingNumber))
        //                     {
        //                         CommonUtility<OPLD>.PushToActiveMQ(opldObject, 1);

        //                         log.Info(DateTime.Now.ToString() + " AMS-POC: OPLD message pushed to MQ1.");
        //                     }
        //                     else {
        //                         log.Warn(DateTime.Now.ToString() + " AMS-POC: Tracking number not found in OPLD message.");
        //                     }
        //                 }
        //             }
        //             else
        //             {
        //                 log.Warn(DateTime.Now.ToString() + " AMS-POC: Tracking number not found in OPLD message.");
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         log.Error(DateTime.Now.ToString() + " AMS-POC: " + Convert.ToString(ex.Message));
        //         return new JsonResult(new { Result = System.Net.HttpStatusCode.InternalServerError });
        //     }

        //     return Ok();
        // }

        //MicroService 2
        [HttpPost]
        public IActionResult MicroServiceProcessOPLDFile(OPLD opldObject)
        {
            try
            {
                //Push OPLD in DB
                //SakilaContext context = HttpContext.RequestServices.GetService(typeof(SakilaContext)) as SakilaContext;
                SakilaContext context = new SakilaContext("server=techm.cooavdyjxzoz.us-east-1.rds.amazonaws.com;port=3306;database=ams;user=root;password=Password123");
                context.AddNewOPLD(opldObject);

                log.Info(DateTime.Now.ToString() + " AMS-POC-MicroServiceProcessOPLDNDIALSFiles: OPLD Data inserted in DB.");

                //Push OPLD in to Active MQ2
                CommonUtility<OPLD>.PushToActiveMQ(opldObject, 1);
                log.Info(DateTime.Now.ToString() + " AMS-MicroServiceProcessOPLDNDIALSFiles: OPLD message pushed to MQ.");

            }
            catch (Exception ex)
            {
                log.Error(DateTime.Now.ToString() + " AMS-MicroServiceProcessOPLDNDIALSFiles: " + Convert.ToString(ex.Message));
                return new JsonResult(new { Result = System.Net.HttpStatusCode.InternalServerError });
            }

            return Ok(new { Result = "Success" });
        }

        [HttpPost]
        public IActionResult MicroServiceProcessDIALSFile(DIALS dialsObject)
        {
            try
            {
                SakilaContext context = new SakilaContext("server=techm.cooavdyjxzoz.us-east-1.rds.amazonaws.com;port=3306;database=ams;user=root;password=Password123");
                context.AddNewDIALS(dialsObject);

            }
            catch (Exception ex)
            {
                log.Error(DateTime.Now.ToString() + " AMS-MicroServiceProcessOPLDNDIALSFiles: " + Convert.ToString(ex.Message));
                return new JsonResult(new { Result = System.Net.HttpStatusCode.InternalServerError });
            }

            return Ok(new { Result = "Success" });
        }
    }
}
