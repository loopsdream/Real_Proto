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

    // 시간 경과에 따른 자동 충전 계산
    if (currentEnergy < maxEnergy) {
        const elapsedMs = serverNow - lastEnergyUpdate;
        const elapsedMinutes = elapsedMs / (1000 * 60);
        const recharged = Math.floor(elapsedMinutes / ENERGY_CONFIG.rechargeMinutes);

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