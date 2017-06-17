﻿using Predictions.Models;
using Predictions.ViewModels.Basis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Predictions.ViewModels
{
    public class EditTourViewModel
    {
        public EditTourViewModel()
        { }

        public EditTourViewModel(List<Team> teams, List<Match> matches, List<FootballScore> scorelist,  TourInfo tourInfo)
        {
            Teamlist = GenerateSelectList(teams);
            TourInfo = tourInfo;
            MatchTable = GenerateMatchTable(matches, scorelist);
            SubmitTextArea = new SubmitTextAreaViewModel(tourInfo.TourId);
        }

        public TourInfo TourInfo { get; set; }
        public MatchTableViewModel MatchTable { get; set; }
        public List<SelectListItem> Teamlist { get; set; }
        public int SelectedHomeTeamId { get; set; }
        public int SelectedAwayTeamId { get; set; }
        public DateTime InputDate { get; set; }
        public SubmitTextAreaViewModel SubmitTextArea { get; set; }

        private List<SelectListItem> GenerateSelectList(List<Team> teams)
        {
            return teams.Select(t => new SelectListItem()
            {
                Text = t.Title,
                Value = t.TeamId.ToString()
            }).ToList();
        }

        private MatchTableViewModel GenerateMatchTable(List<Match> matches, List<FootballScore> scorelist)
        {
            var headers = new List<string>() { "Дата", "Дома", "В гостях", "Счет" };
            var matchlist = matches.Select(m => m.GetMatchInfo()).ToList();

            var actionLinklist = matches.Select(t => 
                new ActionLinkParams(
                    "Удалить", 
                    "DeleteConfirmation", 
                    null, 
                    new { id = t.MatchId }, 
                    new { @class = "btn btn-default" })).ToList();

            return new MatchTableViewModel(headers, matchlist, scorelist, actionLinklist);
        }
    }
}