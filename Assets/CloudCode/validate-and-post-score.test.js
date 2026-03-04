const { describe, it, beforeEach, mock } = require("node:test");
const assert = require("node:assert/strict");

// Mock UGS server-side APIs
function createMockContext(overrides = {}) {
    return {
        projectId: "test-project",
        playerId: "test-player",
        accessToken: "test-token",
        ...overrides
    };
}

function createMockLogger() {
    return { info: () => {}, error: () => {}, warn: () => {} };
}

// Since the Cloud Code script uses require() for UGS modules that aren't
// available in Node, we test the validation logic by extracting and
// reimplementing the handler with injectable dependencies.

const LEADERBOARD_ID = "closest-to-pin";
const MAX_DISTANCE = 115;
const MAX_SUBMISSIONS = 6;

async function validateAndPostScore({ params, context, logger }, dataApi, leaderboardsApi) {
    const { projectId, playerId, accessToken } = context;
    const distance = params.distance;

    if (typeof distance !== "number" || distance < 0 || distance > MAX_DISTANCE) {
        return { success: false, reason: `Distance ${distance} out of range [0, ${MAX_DISTANCE}]` };
    }

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

    await leaderboardsApi.addLeaderboardPlayerScore(
        projectId, LEADERBOARD_ID, playerId,
        { score: distance }
    );

    await dataApi.setItem(projectId, playerId, {
        key: counterKey,
        value: submissionCount + 1
    });

    return { success: true, reason: null };
}

describe("validate-and-post-score", () => {
    let dataApi;
    let leaderboardsApi;
    let logger;

    beforeEach(() => {
        dataApi = {
            getItems: async () => ({ data: { results: [{ value: 0 }] } }),
            setItem: async () => ({})
        };
        leaderboardsApi = {
            addLeaderboardPlayerScore: async () => ({})
        };
        logger = createMockLogger();
    });

    it("accepts valid distance within range", async () => {
        const result = await validateAndPostScore(
            { params: { distance: 5.0 }, context: createMockContext(), logger },
            dataApi, leaderboardsApi
        );
        assert.equal(result.success, true);
    });

    it("rejects negative distance", async () => {
        const result = await validateAndPostScore(
            { params: { distance: -1 }, context: createMockContext(), logger },
            dataApi, leaderboardsApi
        );
        assert.equal(result.success, false);
        assert.ok(result.reason.includes("out of range"));
    });

    it("rejects distance above MAX_DISTANCE", async () => {
        const result = await validateAndPostScore(
            { params: { distance: 120 }, context: createMockContext(), logger },
            dataApi, leaderboardsApi
        );
        assert.equal(result.success, false);
        assert.ok(result.reason.includes("out of range"));
    });

    it("rejects non-numeric distance", async () => {
        const result = await validateAndPostScore(
            { params: { distance: "abc" }, context: createMockContext(), logger },
            dataApi, leaderboardsApi
        );
        assert.equal(result.success, false);
        assert.ok(result.reason.includes("out of range"));
    });

    it("rejects when submission count equals MAX_SUBMISSIONS", async () => {
        dataApi.getItems = async () => ({ data: { results: [{ value: 6 }] } });

        const result = await validateAndPostScore(
            { params: { distance: 5.0 }, context: createMockContext(), logger },
            dataApi, leaderboardsApi
        );
        assert.equal(result.success, false);
        assert.ok(result.reason.includes("Max submissions"));
    });

    it("increments submission counter on success", async () => {
        let setItemCalled = false;
        let savedValue = null;
        dataApi.setItem = async (projectId, playerId, item) => {
            setItemCalled = true;
            savedValue = item.value;
        };

        await validateAndPostScore(
            { params: { distance: 5.0 }, context: createMockContext(), logger },
            dataApi, leaderboardsApi
        );

        assert.equal(setItemCalled, true);
        assert.equal(savedValue, 1); // 0 + 1
    });

    it("handles missing submission counter gracefully", async () => {
        dataApi.getItems = async () => { throw new Error("Not found"); };

        let savedValue = null;
        dataApi.setItem = async (projectId, playerId, item) => {
            savedValue = item.value;
        };

        const result = await validateAndPostScore(
            { params: { distance: 5.0 }, context: createMockContext(), logger },
            dataApi, leaderboardsApi
        );

        assert.equal(result.success, true);
        assert.equal(savedValue, 1); // starts at 0, increments to 1
    });
});
