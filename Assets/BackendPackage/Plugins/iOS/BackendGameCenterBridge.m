#import "BackendGameCenterManager.h"
#import <Foundation/Foundation.h>

#ifdef __cplusplus
extern "C" {
#endif

void BGC_Authenticate() {
    [[BackendGameCenterManager sharedManager] authenticateLocalPlayer];
}

int BGC_IsAuthenticated() {
    return [[BackendGameCenterManager sharedManager] isAuthenticated] ? 1 : 0;
}

void BGC_ReportScore(long long score, const char* leaderboardID) {
    NSString *lb = leaderboardID ? [NSString stringWithUTF8String:leaderboardID] : @"";
    [[BackendGameCenterManager sharedManager] reportScore:score forLeaderboard:lb];
}

void BGC_ShowLeaderboard(const char* leaderboardID) {
    NSString *lb = leaderboardID ? [NSString stringWithUTF8String:leaderboardID] : nil;
    [[BackendGameCenterManager sharedManager] showLeaderboard:lb];
}

void BGC_GetMyScore(const char* leaderboardID, int timeScope) {
    NSString *lb = leaderboardID ? [NSString stringWithUTF8String:leaderboardID] : @"";
    [[BackendGameCenterManager sharedManager] getMyScore:lb timeScope:(GKLeaderboardTimeScope)timeScope];
}

#ifdef __cplusplus
}
#endif
