const { LeaderboardsApi } = require("@unity-services/leaderboards-1.2");
const { DataApi } = require("@unity-services/cloud-save-1.3");

const LEADERBOARD_ID = "closest-to-pin";
const MAX_DISTANCE = 115; // meters
const MAX_SUBMISSIONS = 6;

module.exports = async ({ params, context, logger }) => {
    const { projectId, playerId, accessToken } = context;
    const distance = params.distance;

    // Validate distance range
    if (typeof distance !== "number" || distance < 0 || distance > MAX_DISTANCE) {
        return { success: false, reason: `Distance ${distance} out of range [0, ${MAX_DISTANCE}]` };
    }

    const dataApi = new DataApi({ accessToken });
    const leaderboardsApi = new LeaderboardsApi({ accessToken });

    // Check submission count from Cloud Save
    const counterKey = "submission_count";
    let submissionCount = 0;
    try {
        const saved = await dataApi.getItems(projectId, playerId, [counterKey]);
        if (saved.data.results.length > 0) {
            submissionCount = saved.data.results[0].value;
        }
    } catch (e) {
        logger.info("No existing submission count, starting at 0");
    }

    if (submissionCount >= MAX_SUBMISSIONS) {
        return { success: false, reason: `Max submissions (${MAX_SUBMISSIONS}) reached` };
    }

    // Write score to leaderboard (UGS keeps best = lowest via sort order config)
    await leaderboardsApi.addLeaderboardPlayerScore(
        projectId, LEADERBOARD_ID, playerId,
        { score: distance }
    );

    // Increment submission counter
    await dataApi.setItem(projectId, playerId, {
        key: counterKey,
        value: submissionCount + 1
    });

    return { success: true, reason: null };
};

module.exports.params = {
    distance: { type: "NUMERIC", required: true }
};
