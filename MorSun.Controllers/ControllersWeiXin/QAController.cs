﻿using MorSun.Common.配置;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MorSun.Controllers.ViewModel;
using HOHO18.Common;
using MorSun.Bll;
using MorSun.Model;
using HOHO18.Common.WEB;
using MorSun.Common.类别;

namespace MorSun.Controllers
{    
    [HandleError]
    public class QAController : BasisController
    {
        public ActionResult Q(Guid? id)
        {
            LogHelper.Write(Request.RawUrl, LogHelper.LogMessageType.Info);
            var model = new BMQAViewVModel();
            if(id != null)
            { 
                model.sParentId = id;
                return View(model);
            }    
            else
            {
                return RedirectToAction("I", "H");
            }
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MB(Guid? id, int? qCount,string returnUrl)
        {
            var s = "";
            var tempMB = Convert.ToDecimal(0);
            var tempBB = Convert.ToDecimal(0);
            var defXFMB = Convert.ToDecimal(CFG.提问默认收费马币值);
            if (!qCount.HasValue)
                qCount = 1;
            else
            {
                qCount = Math.Abs(qCount.Value);
                if (qCount == 0)
                    qCount = 1;
            }
            if(!id.HasValue || id==Guid.Empty)
            {
                "qCount".AE("参数错误", ModelState);
                s += "参数错误";
            }
            else 
            { 
                //var qa = new BaseBll<bmQA>().GetModel(id);
                var bmqaBll = new BaseBll<bmQAView>();
                var qaView = bmqaBll.GetModel(id);
                if (qaView == null)
                {
                    "qCount".AE("参数错误", ModelState);
                    s += "参数错误";
                }
                var qauser = new BaseBll<bmUserWeixin>().All.FirstOrDefault(p => p.WeiXinId == qaView.WeiXinId);
                if(qauser == null)
                {
                    "qCount".AE("未绑定", ModelState);
                    s += " 提问用户未绑定邦马网";
                }
                else if(qauser.UserId != UserID)
                {
                    "qCount".AE("不是您的", ModelState);
                    s += " 不是您的问题你别动";
                }

                LogHelper.Write(qaView.MBNum.ToString() + qaView.BBNum.ToString(), LogHelper.LogMessageType.Debug);
                if ((Math.Abs(qaView.MBNum) + Math.Abs(qaView.BBNum) + qCount * defXFMB)  >= 25000)
                {//超过25000马币就不让再增加
                    "qCount".AE("超过50", ModelState);
                    s += "问题总数量不能超过50";
                }
                //已经被回答了则不再增加马币
                var refAId = Guid.Parse(Reference.问答类别_答案);
                var refBSId = Guid.Parse(Reference.问答类别_不是问题);
                var qada = bmqaBll.All.FirstOrDefault(p => p.ParentId == id && (p.QARef == refAId || p.QARef == refBSId));
                if(qada != null)
                {
                    "qCount".AE("已被解答", ModelState);
                    s += " 该问题已经被解答";
                }
                //邦马币余额不足
                var numbbll = new BaseBll<bmNewUserMB>();                
                var UserBMB = numbbll.All.FirstOrDefault(p => p.UserId == UserID);
                tempMB = UserBMB.NMB.Value;
                tempBB = UserBMB.NBB.Value;
                if ((tempMB + tempBB) < qCount * defXFMB)
                {
                    "qCount".AE("余额不足", ModelState);
                    s += " 您的邦马币余额不足";
                }
            }
            var oper = new OperationResult(OperationResultType.Error, "提交失败：" + s);
            if (ModelState.IsValid)
            {
                var bmumbBll = new BaseBll<bmUserMaBiRecord>();
                for(var i = 0; i<qCount; i++)
                { 
                    var umbrModel = new bmUserMaBiRecord();
                    umbrModel.SourceRef = Guid.Parse(Reference.马币来源_消费);
                
                    if (tempBB >= defXFMB)
                    {
                        umbrModel.MaBiRef = Guid.Parse(Reference.马币类别_邦币);
                        tempBB -= defXFMB;
                    }
                    else if (tempMB >= defXFMB)
                    {
                        umbrModel.MaBiRef = Guid.Parse(Reference.马币类别_马币);
                        tempMB -= defXFMB;
                    }
                    umbrModel.MaBiNum = 0 - defXFMB;
                    umbrModel.QAId = id;

                    umbrModel.IsSettle = false;
                    umbrModel.RegTime = DateTime.Now;
                    umbrModel.ModTime = DateTime.Now;
                    umbrModel.FlagTrashed = false;
                    umbrModel.FlagDeleted = false;

                    umbrModel.ID = Guid.NewGuid();
                    umbrModel.UserId = UserID;
                    umbrModel.RegUser = UserID;

                    bmumbBll.Insert(umbrModel,false);
                }
                bmumbBll.UpdateChanges();
                //封装返回的数据
                fillOperationResult(returnUrl, oper, "马币增加成功");
                return Json(oper, JsonRequestBehavior.AllowGet);
            }
            oper.AppendData = ModelState.GE();
            return Json(oper, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OB(AddObjection t, string returnUrl)
        {
            var s = "";
            var tempMB = Convert.ToDecimal(0);
            var tempBB = Convert.ToDecimal(0);
            var defXFMB = Convert.ToDecimal(CFG.提问默认收费马币值);
            
            t.ErrorNum = Math.Abs(t.ErrorNum);
            if (t.ErrorNum == 0)
                t.ErrorNum = 1;            
            if (t.QAId == Guid.Empty)
            {
                "ErrorNum".AE("参数错误", ModelState);
                s += "参数错误";
            }
            else
            {
                //var qa = new BaseBll<bmQA>().GetModel(id);
                var bmqaBll = new BaseBll<bmQAView>();
                var qaView = bmqaBll.GetModel(t.QAId);
                if (qaView == null)
                {
                    "ErrorNum".AE("参数错误", ModelState);
                    s += "参数错误";
                }
                else
                {
                    if(Math.Abs(qaView.MBNum) + Math.Abs(qaView.BBNum) == 0)
                    {
                        "ErrorNum".AE("该问题是免费提问，您不能提交异议", ModelState);
                        s += "该问题是免费提问，您不能提交异议";
                    }
                }
                var qauser = new BaseBll<bmUserWeixin>().All.FirstOrDefault(p => p.WeiXinId == qaView.WeiXinId);
                if (qauser == null)
                {
                    "ErrorNum".AE("提问用户未绑定邦马网", ModelState);
                    s += "提问用户未绑定邦马网";
                }
                else if (qauser.UserId != UserID)
                {
                    "ErrorNum".AE("不是您的问题你别动", ModelState);
                    s += " 不是您的问题你别动";
                }

                LogHelper.Write(qaView.MBNum.ToString() + qaView.BBNum.ToString(), LogHelper.LogMessageType.Debug);
                if (t.ErrorNum > 50)
                {//超过25000马币就不让再增加
                    "ErrorNum".AE("问题总数量不能超过50", ModelState);
                    s += "问题总数量不能超过50";
                }
                //已经被回答了则不再增加马币
                var refAId = Guid.Parse(Reference.问答类别_答案);
                var refBSId = Guid.Parse(Reference.问答类别_不是问题);
                var qada = bmqaBll.All.FirstOrDefault(p => p.ParentId == t.QAId && (p.QARef == refAId || p.QARef == refBSId));
                if (qada == null)
                {
                    "ErrorNum".AE("该问题未解答", ModelState);
                    s += " 该问题未解答";
                }
                else
                { 
                    //从问题解答开始到现在已经超过72个小时
                    var yyjg = Convert.ToInt32(CFG.用户提交异议有效时间间隔);                    
                    if (qada.RegTime.Value.AddHours(yyjg) < DateTime.Now)
                    {
                        "ErrorNum".AE("超期提交无效", ModelState);
                        s += "该问题解答时间到现在已超过" + yyjg + "小时，不能再提交异议";
                    }
                }
                var ob = new BaseBll<bmObjection>().All.FirstOrDefault(p => p.QAId == t.QAId);
                if(ob != null)
                {
                    "ErrorNum".AE("该问题已经提交了一次异议", ModelState);
                    s += " 该问题已经提交了一次异议";
                }
                //邦马币余额不足
                var numbbll = new BaseBll<bmNewUserMB>();
                var UserBMB = numbbll.All.FirstOrDefault(p => p.UserId == UserID);
                tempMB = UserBMB.NMB.Value;
                tempBB = UserBMB.NBB.Value;
                if ((tempMB + tempBB) < t.ErrorNum * defXFMB)
                {
                    "ErrorNum".AE("您的邦马币余额不足", ModelState);
                    s += " 您的邦马币余额不足";
                }
            }
            
            var oper = new OperationResult(OperationResultType.Error, "提交失败：" + s);
            if (ModelState.IsValid)
            {
                //添加异议
                var obBll = new BaseBll<bmObjection>();
                var model = new bmObjection();
                TryUpdateModel(model);
                model.ID = Guid.NewGuid();
                model.UserId = UserID;
                model.SubmitTime = DateTime.Now;

                model.IsSettle = false;
                model.RegTime = DateTime.Now;
                model.ModTime = DateTime.Now;
                model.FlagTrashed = false;
                model.FlagDeleted = false;
                obBll.Insert(model);

                //马币扣取
                var bmumbBll = new BaseBll<bmUserMaBiRecord>();
                for (var i = 0; i < t.ErrorNum; i++)
                {
                    var umbrModel = new bmUserMaBiRecord();
                    umbrModel.SourceRef = Guid.Parse(Reference.马币来源_扣取);

                    if (tempBB >= defXFMB)
                    {
                        umbrModel.MaBiRef = Guid.Parse(Reference.马币类别_邦币);
                        tempBB -= defXFMB;
                    }
                    else if (tempMB >= defXFMB)
                    {
                        umbrModel.MaBiRef = Guid.Parse(Reference.马币类别_马币);
                        tempMB -= defXFMB;
                    }
                    umbrModel.MaBiNum = 0 - defXFMB;
                    umbrModel.QAId = t.QAId;
                    umbrModel.OBId = model.ID;

                    umbrModel.IsSettle = false;
                    umbrModel.RegTime = DateTime.Now;
                    umbrModel.ModTime = DateTime.Now;
                    umbrModel.FlagTrashed = false;
                    umbrModel.FlagDeleted = false;

                    umbrModel.ID = Guid.NewGuid();
                    umbrModel.UserId = UserID;
                    umbrModel.RegUser = UserID;

                    bmumbBll.Insert(umbrModel, false);
                }
                bmumbBll.UpdateChanges();
                //封装返回的数据
                fillOperationResult(returnUrl, oper, "提交成功");
                return Json(oper, JsonRequestBehavior.AllowGet);
            }


            oper.AppendData = ModelState.GE();
            return Json(oper, JsonRequestBehavior.AllowGet);
        }

    }
}
