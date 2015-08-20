/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import * as xp_charts from './charts.js';
import {Parse} from 'parse';
import React from 'react';
import GitHub from 'github-api';

class Controller extends xp_common.Controller {

	pullRequestId: string | void;

	constructor (pullRequestId) {
		super ();
		this.pullRequestId = pullRequestId;
	}

	allDataLoaded () {
        if (this.pullRequestId === undefined)
            return;

        var prRunSet = this.runSetForPullRequestId (this.pullRequestId);
        var pullRequest = prRunSet.get ('pullRequest');
        var baselineRunSet = pullRequest.get ('baselineRunSet');

        if (baselineRunSet === undefined || prRunSet === undefined) {
            console.log ("Error: Could not get baseline or test run set.", baselineRunSet, prRunSet);
            return;
        }

        var runSets = [baselineRunSet, prRunSet];

        var machine = this.machineForId (prRunSet.get ('machine').id);
        var config = this.configForId (prRunSet.get ('config').id);

		React.render (
			<div className="PullRequestPage">
				<xp_common.Navigation currentPage="" />
                <article>
                    <div className="panel">
                        <PullRequestDescription
                            pullRequest={pullRequest} />
                        <xp_common.MachineDescription
                            machine={machine}
                            omitHeader={false} />
                        <xp_common.ConfigDescription
                            config={config}
                            omitHeader={false} />
                    </div>
                    <xp_charts.ComparisonAMChart
                        graphName="comparisonChart"
                        controller={this}
                        runSets={runSets} />
                </article>
			</div>,
			document.getElementById ('pullRequestPage')
		);
	}
}

type PullRequestDescriptionProps = {
	pullRequest: Parse.Object;
};

class PullRequestDescription extends React.Component<PullRequestDescriptionProps, PullRequestDescriptionProps, void> {
    constructor (props) {
        super (props);
        this.state = { title: undefined };

        var pr = this.props.pullRequest;
        var match = pr.get ('URL').match (/^https?:\/\/github\.com\/mono\/mono\/pull\/(\d+)\/?$/);
        if (match !== null) {
            var github = new GitHub ({});
            var repo = github.getRepo ("mono", "mono");
            repo.getPull (match [1], (err, info) => {
                console.log ("got pull: ", info);
                if (info.title !== undefined)
                    this.setState ({ title: info.title });
            });
        }
    }

	render () : Object {
		var pr = this.props.pullRequest;

        var baselineRunSet = pr.get ('baselineRunSet');
        var baselineHash = baselineRunSet.get ('commit').get ('hash');

        var title = "Link";
        if (this.state.title !== undefined)
            title = this.state.title;

		return <div className="Description">
			<dl>
			<dt>Pull Request</dt>
            <dd><a href={pr.get ('URL')}>{title}</a></dd>
			<dt>Baseline commit</dt>
			<dd><a href={xp_common.githubCommitLink (baselineHash)}>{baselineHash.substring (0, 10)}</a></dd>
			</dl>
		</div>;
	}
}

function started () {
	var pullRequestId;
	if (window.location.hash)
		pullRequestId = window.location.hash.substring (1);
	var controller = new Controller (pullRequestId);
	controller.loadAsync ();
}

xp_common.start (started);
