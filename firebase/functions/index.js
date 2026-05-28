const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { initializeApp } = require("firebase-admin/app");
const { getDatabase } = require("firebase-admin/database");

initializeApp();

// 재화 종류 유효성 체크
const VALID_CURRENCIES = ["gameCoins", "diamonds"];

// 재화 소비 검증 Function
// force redeploy v2
exports.spendCurrency = onCall({ region: "asia-northeast3" }, async (request) => {
    // 로그인 여부 체크
    if (!request.auth) {
        throw new HttpsError("unauthenticated", "Login required");
    }

    const userId = request.auth.uid;
    const { currencyType, amount, reason } = request.data;

    // 입력값 검증
    if (!VALID_CURRENCIES.includes(currencyType)) {
        throw new HttpsError("invalid-argument", "Invalid currency type");
    }
    if (!Number.isInteger(amount) || amount <= 0) {
        throw new HttpsError("invalid-argument", "Amount must be positive integer");
    }
    if (!reason) {
        throw new HttpsError("invalid-argument", "Reason is required");
    }

    const db = getDatabase();
    const currencyRef = db.ref(`users/${userId}/currencies/${currencyType}`);

    // [수정] abort 대신 플래그 방식으로 잔액 부족 처리
    let insufficientBalance = false;

    const result = await currencyRef.transaction((currentValue) => {
        const balance = currentValue || 0;
        if (balance < amount) {
            insufficientBalance = true;
            return balance; // 값 변경 없이 그대로 반환 (abort 안 함)
        }
        insufficientBalance = false;
        return balance - amount;
    });

    if (insufficientBalance) {
        throw new HttpsError("failed-precondition", "Insufficient balance");
    }

    const newBalance = result.snapshot.val();
    console.log(`[spendCurrency] userId=${userId} type=${currencyType} amount=${amount} reason=${reason} newBalance=${newBalance}`);

    return {
        success: true,
        newBalance: newBalance,
        currencyType: currencyType
    };
});

// 재화 획득 검증 Function (광고 보상, 스테이지 클리어 보상 등)
exports.addCurrency = onCall({ region: "asia-northeast3" }, async (request) => {
    // 로그인 여부 체크
    if (!request.auth) {
        throw new HttpsError("unauthenticated", "Login required");
    }

    const userId = request.auth.uid;
    const { currencyType, amount, reason } = request.data;

    // 입력값 검증
    if (!VALID_CURRENCIES.includes(currencyType)) {
        throw new HttpsError("invalid-argument", "Invalid currency type");
    }
    if (!Number.isInteger(amount) || amount <= 0) {
        throw new HttpsError("invalid-argument", "Amount must be positive integer");
    }

    // 비정상적으로 큰 금액 차단 (치트 방지)
    const MAX_REWARD = { gameCoins: 10000, diamonds: 100 };
    if (amount > MAX_REWARD[currencyType]) {
        throw new HttpsError("invalid-argument", "Amount exceeds maximum reward limit");
    }

    if (!reason) {
        throw new HttpsError("invalid-argument", "Reason is required");
    }

    const db = getDatabase();
    const currencyRef = db.ref(`users/${userId}/currencies/${currencyType}`);

    const result = await currencyRef.transaction((currentValue) => {
        return (currentValue || 0) + amount;
    });

    const newBalance = result.snapshot.val();
    console.log(`[addCurrency] userId=${userId} type=${currencyType} amount=${amount} reason=${reason} newBalance=${newBalance}`);

    return {
        success: true,
        newBalance: newBalance,
        currencyType: currencyType
    };
});

// ============================================
// 에너지 시스템 설정
// ============================================
const ENERGY_CONFIG = {
    maxEnergy: 5,
    rechargeMinutes: 10  // 10분마다 1 충전
};

// 에너지 소비 검증 Function
exports.spendEnergy = onCall({ region: "asia-northeast3" }, async (request) => {
    if (!request.auth) {
        throw new HttpsError("unauthenticated", "Login required");
    }

    const userId = request.auth.uid;
    const { amount, reason } = request.data;

    // 입력값 검증
    if (!Number.isInteger(amount) || amount <= 0) {
        throw new HttpsError("invalid-argument", "Amount must be positive integer");
    }
    if (!reason) {
        throw new HttpsError("invalid-argument", "Reason is required");
    }

    const db = getDatabase();
    const energyRef = db.ref(`users/${userId}/currencies`);

    // 서버 현재 시간 (밀리초)
    const serverNow = Date.now();

    const snapshot = await energyRef.once("value");
    const currencies = snapshot.val() || {};

    let currentEnergy = currencies.energy || 0;
    const maxEnergy = currencies.maxEnergy || ENERGY_CONFIG.maxEnergy;
    const lastEnergyUpdate = currencies.lastEnergyUpdateServer || serverNow;

    let recharged = 0;

    // 시간 경과에 따른 자동 충전 계산
    if (currentEnergy < maxEnergy) {
        const elapsedMs = serverNow - lastEnergyUpdate;
        const elapsedMinutes = elapsedMs / (1000 * 60);
        recharged = Math.floor(elapsedMinutes / ENERGY_CONFIG.rechargeMinutes);

        if (recharged > 0) {
            currentEnergy = Math.min(currentEnergy + recharged, maxEnergy);
        }
    }

    // 잔액 확인
    if (currentEnergy < amount) {
        throw new HttpsError("failed-precondition",
            `Insufficient energy: have ${currentEnergy}, need ${amount}`);
    }

    // 차감 및 저장
    const newEnergy = currentEnergy - amount;

    // 최대치에서 소비했으면 충전 타이머 시작
    const updateData = {
        energy: newEnergy,
        lastEnergyUpdateServer: serverNow
    };

    await energyRef.update(updateData);

    // 다음 충전까지 남은 시간 계산
    let nextRechargeMs = 0;
    if (newEnergy < maxEnergy) {
        nextRechargeMs = ENERGY_CONFIG.rechargeMinutes * 60 * 1000;
    }

    console.log(`[spendEnergy] DB energy=${currencies.energy}, lastUpdate=${lastEnergyUpdate}, recharged=${recharged || 0}, afterRecharge=${currentEnergy}, newEnergy=${newEnergy}`);
    console.log(`[spendEnergy] userId=${userId} amount=${amount} reason=${reason} newEnergy=${newEnergy}`);

    return {
        success: true,
        newEnergy: newEnergy,
        maxEnergy: maxEnergy,
        serverTime: serverNow,
        nextRechargeMs: nextRechargeMs
    };
});

// 에너지 획득 Function (광고 보상 등)
exports.addEnergy = onCall({ region: "asia-northeast3" }, async (request) => {
    if (!request.auth) {
        throw new HttpsError("unauthenticated", "Login required");
    }

    const userId = request.auth.uid;
    const { amount, reason } = request.data;

    if (!Number.isInteger(amount) || amount <= 0 || amount > 5) {
        throw new HttpsError("invalid-argument", "Amount must be 1-5");
    }
    if (!reason) {
        throw new HttpsError("invalid-argument", "Reason is required");
    }

    const db = getDatabase();
    const energyRef = db.ref(`users/${userId}/currencies`);

    const serverNow = Date.now();

    const snapshot = await energyRef.once("value");
    const currencies = snapshot.val() || {};

    let currentEnergy = currencies.energy || 0;
    const maxEnergy = currencies.maxEnergy || ENERGY_CONFIG.maxEnergy;
    const lastEnergyUpdate = currencies.lastEnergyUpdateServer || serverNow;

    // 시간 경과에 따른 자동 충전 먼저 적용
    if (currentEnergy < maxEnergy) {
        const elapsedMs = serverNow - lastEnergyUpdate;
        const elapsedMinutes = elapsedMs / (1000 * 60);
        const recharged = Math.floor(elapsedMinutes / ENERGY_CONFIG.rechargeMinutes);

        if (recharged > 0) {
            currentEnergy = Math.min(currentEnergy + recharged, maxEnergy);
        }
    }

    // 에너지 추가 (최대치 제한)
    const newEnergy = Math.min(currentEnergy + amount, maxEnergy);

    const updateData = {
        energy: newEnergy,
        lastEnergyUpdateServer: serverNow
    };

    await energyRef.update(updateData);

    console.log(`[addEnergy] userId=${userId} amount=${amount} reason=${reason} newEnergy=${newEnergy}`);

    return {
        success: true,
        newEnergy: newEnergy,
        maxEnergy: maxEnergy,
        serverTime: serverNow
    };
});

// ============================================
// 스테이지 클리어 검증 Function
// ============================================
exports.clearStage = onCall({ region: "asia-northeast3" }, async (request) => {
    if (!request.auth) {
        throw new HttpsError("unauthenticated", "Login required");
    }

    const userId = request.auth.uid;
    const { stageNumber, score, stars, rewards } = request.data;

    // 입력값 검증
    if (!Number.isInteger(stageNumber) || stageNumber < 1) {
        throw new HttpsError("invalid-argument", "Invalid stage number");
    }
    if (!Number.isInteger(score) || score < 0) {
        throw new HttpsError("invalid-argument", "Invalid score");
    }
    if (!Number.isInteger(stars) || stars < 0 || stars > 3) {
        throw new HttpsError("invalid-argument", "Stars must be 0-3");
    }

    const db = getDatabase();
    const userRef = db.ref(`users/${userId}`);

    const snapshot = await userRef.once("value");
    const userData = snapshot.val() || {};
    const stageProgress = userData.stageProgress || {};
    const currencies = userData.currencies || {};

    // 현재 스테이지 진행도 확인 - 이전 스테이지를 클리어했는지 검증
    const currentStage = userData.currentStage || 1;

    // 이전 클리어 기록 확인
    const stageKey = `stage_${stageNumber}`;
    const prevProgress = stageProgress[stageKey] || {};
    const isFirstClear = !prevProgress.completed;

    // 보상 계산 및 지급
    let coinReward = 0;
    let diamondReward = 0;
    let energyReward = 0;
    const itemRewards = {};

    if (rewards && Array.isArray(rewards)) {
        for (const reward of rewards) {
            switch (reward.type) {
                case "Coins":
                    coinReward += reward.amount || 0;
                    break;
                case "Diamonds":
                    diamondReward += reward.amount || 0;
                    break;
                case "Energy":
                    energyReward += reward.amount || 0;
                    break;
                case "Hammer":
                case "Tornado":
                case "Brush":
                    itemRewards[reward.type] = (itemRewards[reward.type] || 0) + (reward.amount || 0);
                    break;
            }
        }
    }

    // 보상 상한 체크 (치트 방지)
    if (coinReward > 5000 || diamondReward > 50 || energyReward > 5) {
        throw new HttpsError("invalid-argument", "Reward exceeds maximum limit");
    }

    // 업데이트 데이터 구성
    const updates = {};

    // 스테이지 진행도 업데이트
    updates[`stageProgress/${stageKey}/completed`] = true;
    updates[`stageProgress/${stageKey}/stageNumber`] = stageNumber;
    updates[`stageProgress/${stageKey}/completedTime`] = Date.now();

    // 최고 기록 갱신
    if (score > (prevProgress.bestScore || 0)) {
        updates[`stageProgress/${stageKey}/bestScore`] = score;
    }
    if (stars > (prevProgress.bestStars || 0)) {
        updates[`stageProgress/${stageKey}/bestStars`] = stars;
    }

    // 다음 스테이지 해금
    if (stageNumber >= currentStage) {
        updates["currentStage"] = stageNumber + 1;
    }

    // 재화 지급
    if (coinReward > 0) {
        updates["currencies/gameCoins"] = (currencies.gameCoins || 0) + coinReward;
    }
    if (diamondReward > 0) {
        updates["currencies/diamonds"] = (currencies.diamonds || 0) + diamondReward;
    }
    if (energyReward > 0) {
        const newEnergy = Math.min(
            (currencies.energy || 0) + energyReward,
            currencies.maxEnergy || 5
        );
        updates["currencies/energy"] = newEnergy;
    }

    // 아이템 지급
    if (itemRewards["Hammer"]) {
        updates["currencies/hammerCount"] = (currencies.hammerCount || 0) + itemRewards["Hammer"];
    }
    if (itemRewards["Tornado"]) {
        updates["currencies/tornadoCount"] = (currencies.tornadoCount || 0) + itemRewards["Tornado"];
    }
    if (itemRewards["Brush"]) {
        updates["currencies/brushCount"] = (currencies.brushCount || 0) + itemRewards["Brush"];
    }

    await userRef.update(updates);

    const newCoins = (currencies.gameCoins || 0) + coinReward;
    const newDiamonds = (currencies.diamonds || 0) + diamondReward;
    const newCurrentStage = stageNumber >= currentStage ? stageNumber + 1 : currentStage;

    console.log(`[clearStage] userId=${userId} stage=${stageNumber} stars=${stars} score=${score} isFirst=${isFirstClear} coins=+${coinReward} diamonds=+${diamondReward}`);

    return {
        success: true,
        stageNumber: stageNumber,
        isFirstClear: isFirstClear,
        newCurrentStage: newCurrentStage,
        newCoins: newCoins,
        newDiamonds: newDiamonds,
        bestScore: Math.max(score, prevProgress.bestScore || 0),
        bestStars: Math.max(stars, prevProgress.bestStars || 0)
    };
});

// 스테이지 진행도 초기화 Function (디버그용)
exports.resetStageProgress = onCall({ region: "asia-northeast3" }, async (request) => {
    if (!request.auth) {
        throw new HttpsError("unauthenticated", "Login required");
    }

    const userId = request.auth.uid;
    const db = getDatabase();
    const userRef = db.ref(`users/${userId}`);

    await userRef.update({
        "stageProgress": null,
        "currentStage": 1
    });

    console.log(`[resetStageProgress] userId=${userId} - All stage progress reset`);

    return {
        success: true,
        message: "Stage progress reset to stage 1"
    };
});