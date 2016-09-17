﻿using Predictions.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Predictions.Models;
using Predictions.ViewModels;
using Predictions.Services;
using System.Net;
using System.Data.Entity;
using Predictions.ViewModels.Basis;
using Predictions.Helpers;

namespace Predictions.Controllers
{
    public class ToursController : Controller
    {
        private readonly PredictionsContext _context;
        private readonly ExpertService _expertService;
        private readonly TourService _tourService;
        private readonly PredictionService _predictionService;
        private readonly MatchService _matchService;
        private readonly TeamService _teamService;

        public ToursController()
        {
            _context = new PredictionsContext();
            _expertService = new ExpertService(_context);
            _tourService = new TourService(_context);
            _predictionService = new PredictionService(_context);
            _matchService = new MatchService(_context);
            _teamService = new TeamService(_context);
        }

        public ActionResult Index()
        {
            var tours = _tourService.LoadBasicsWith();
            if (tours == null) return HttpNotFound();
            //foreach (var item in experts)
            //{
            //    item.Differences = 0;
            //    item.Outcomes = 0;
            //    item.Scores = 0;
            //}
            //_context.SaveChanges();
            return View(tours);
        }

        //404 after deleting
        public ActionResult EditTour(int tourId)
        {
            //doesn't need
            //Tour tour = _tourService.LoadBasicsWith(tourId);
            //if (tour == null) return HttpNotFound();
            var tourInfo = _tourService.GetTourInfo(tourId);
            var teamlist = _teamService.GenerateSelectList();
            var headers = new List<string>() { "Дата", "Дома", "В гостях", "Счет" };
            var matchlist = _matchService.GenerateMatchlist(tourId);
            var scorelist = _matchService.GenerateScorelist(tourId);

            //to some service
            var actionLinklist = new List<ActionLinkParams>();
            var matches = _tourService.GetMatchesByTour(tourId);
            for (var i = 0; i < matches.Count; i++)
            {
                var actionLink = new ActionLinkParams("Удалить", "DeleteConfirmation", null, new { id = matches[i].MatchId }, new { @class = "btn btn-default" });
                actionLinklist.Add(actionLink);
            }

            var matchTable = new MatchTableViewModel(headers, matchlist, scorelist, actionLinklist);

            return View(new EditTourViewModel(teamlist, tourInfo, matchTable));
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "SaveTourSettings")]
        public ActionResult SaveTourSettings(EditTourViewModel viewModel)
        {
            //id nullcheck
            if (ModelState.IsValid)
            {
                _tourService.UpdateTour(viewModel.TourInfo);
                return RedirectToAction("Index");
            }
            return EditTour(viewModel.TourInfo.TourId);

        }

        //bind include
        //TODO: real tour update
        [HttpPost]
        [MultipleButton(Name = "action", Argument = "AddMatch")]
        public ActionResult AddMatch(EditTourViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var match = _matchService.CreateMatch(
                    viewModel.InputDate,
                    viewModel.SelectedHomeTeamId,
                    viewModel.SelectedAwayTeamId,
                    viewModel.TourInfo.TourId);
                _matchService.AddMatch(match);
                return RedirectToAction("EditTour", new {tourId = viewModel.TourInfo.TourId });
            }
            return View(viewModel);
        }

        public ActionResult AddPredictions(int? tourId, int? expertId)
        {
            if (tourId == null) return HttpNotFound();
            var expertlist = _expertService.GenerateSelectList();
            var tourInfo = _tourService.GetTourInfo(tourId);

            var headers = new List<string>() { "Дата", "Дома", "В гостях", "Прогноз" };
            var matchlist = _matchService.GenerateMatchlist(tourId);
            var scorelist = _predictionService.GeneratePredictionlist(tourId, expertId, true);

            var matchTable = new MatchTableViewModel(headers, matchlist, scorelist);
            var viewModel = new AddPredictionsViewModel(expertlist, tourInfo, matchTable);
            viewModel.SelectedExpertId = expertId ?? 0;
            return View(viewModel);
        }

        //bind
        [HttpPost]
        [MultipleButton(Name = "action", Argument = "ShowPredictions")]
        public ActionResult ShowPredictions(AddPredictionsViewModel viewModel)
        {
            return RedirectToAction("AddPredictions", new { tourId = viewModel.TourInfo.TourId, expertId = viewModel.SelectedExpertId });
        }

        //bind
        [HttpPost]
        [MultipleButton(Name = "action", Argument = "AddPredictions")]
        public ActionResult AddPredictions(AddPredictionsViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                _predictionService.AddExpertPredictions(viewModel.SelectedExpertId, viewModel.TourInfo.TourId, viewModel.MatchTable.Scorelist);
                return RedirectToAction("AddPredictions", new { tourId = viewModel.TourInfo.TourId, expertId = viewModel.SelectedExpertId });
            }
            return View(viewModel);

        }

        public ActionResult AddScores(int? tourId)
        {
            var matchlist = _matchService.GenerateMatchlist(tourId);
            var scorelist = _matchService.GenerateScorelist(tourId, true);
            if (matchlist == null || scorelist == null) return HttpNotFound();
            var headers = new List<string>() { "Дата", "Дома", "В гостях", "Счет" };
            var matchTable = new MatchTableViewModel(headers, matchlist, scorelist);

            return View(new AddScoresViewModel(tourId.Value, matchTable));
        }

        [HttpPost]
        public ActionResult AddScores([Bind(Include = "MatchTable, CurrentTourId")] AddScoresViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                _matchService.AddScores(_tourService.GetMatchesByTour(viewModel.CurrentTourId), viewModel.MatchTable.Scorelist);
                return RedirectToAction("Index");
            }
            return AddScores(viewModel.CurrentTourId); //not sure
        }

        public ActionResult Preresults(int tourId)
        {
            var preresults = _tourService.GenerateTourPreresultlist(tourId);
            var enableSubmit = _tourService.AllResultsReady(tourId);
            var viewModel = new PreresultsViewModel(preresults, _tourService.MatchesCount(tourId), tourId, enableSubmit);
            return View(viewModel);
        }

        public ActionResult SubmitTourPredictions(int tourId)
        {
            _predictionService.SubmitTourPredictions(tourId);
            return RedirectToAction("Index");
        }

        public ActionResult RestartTour(int tourId)
        {
            _predictionService.RestartTour(tourId);
            return RedirectToAction("Index");
        }

        //404
        public ActionResult DeleteMatch(int id)
        {
            //so~so, mb better
            var tourId = _matchService.GetTourId(id);
            if (tourId == null) return HttpNotFound(); //this also checks id
            _matchService.DeleteMatch(id);
            return RedirectToAction("EditTour", new { id = tourId });
        }

        //terrible, fix as fast as possible
        public ActionResult DeleteConfirmation(int id)
        {
            var match = _context.Matches.Find(id);

            ViewBag.number = match.Predictions.IsNullOrEmpty() ? 0 : match.Predictions.Count();
            ViewBag.id = id;
            return View();
        }

        //public ActionResult TestAction()
        //{
        //    var tour = _tourService.LoadBasicsWith(1);
        //    var matches = tour.Matches.ToList();
        //    var matchlist = _matchService.GenerateMatchlist(matches);
        //    var scorelist = _matchService.GenerateScorelist(matches, true);
        //    var headers = new List<string>(){"Дата", "Дома", "В гостях", "Счет"};
        //    var table = new MatchTableViewModel(headers, matchlist, scorelist);
        //    return View(table);
        //}
    }
}