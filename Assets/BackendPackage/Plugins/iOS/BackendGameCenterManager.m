#import "BackendGameCenterManager.h"

extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

@implementation BackendGameCenterManager

+ (instancetype)sharedManager {
    static BackendGameCenterManager *s_instance;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        s_instance = [[BackendGameCenterManager alloc] init];
    });
    return s_instance;
}

- (void)authenticateLocalPlayer {
    dispatch_async(dispatch_get_main_queue(), ^{
        __weak __typeof(self) weakSelf = self;

        [GKLocalPlayer.localPlayer setAuthenticateHandler:^(UIViewController * _Nullable viewController, NSError * _Nullable error) {
            if (viewController) {
                UIWindow *activeWindow = nil;
                for (UIWindowScene *scene in UIApplication.sharedApplication.connectedScenes) {
                    if (scene.activationState == UISceneActivationStateForegroundActive) {
                        for (UIWindow *window in scene.windows) {
                            if (window.isKeyWindow) {
                                activeWindow = window;
                                break;
                            }
                        }
                        if (activeWindow) break;
                    }
                }
                if (!activeWindow) {
                    activeWindow = UIApplication.sharedApplication.windows.firstObject;
                }

                UIViewController *root = activeWindow.rootViewController;
                if (root.presentedViewController) {
                    [root.presentedViewController presentViewController:viewController animated:YES completion:nil];
                } else {
                    [root presentViewController:viewController animated:YES completion:nil];
                }
            } else if (GKLocalPlayer.localPlayer.isAuthenticated) {
                BGC_SendCallback("BackendGameCenterListener", "OnAuthSuccess", "");
            } else {
                NSString *err = error ? error.localizedDescription : @"Authentication failed";
                BGC_SendCallback("BackendGameCenterListener", "OnAuthFailed", [err UTF8String]);
            }

            __unused __typeof(weakSelf) strongSelf = weakSelf;
        }];
    });
}

- (BOOL)isAuthenticated {
    return GKLocalPlayer.localPlayer.isAuthenticated;
}

- (void)reportScore:(int64_t)score forLeaderboard:(NSString*)leaderboardID {
    if (!GKLocalPlayer.localPlayer.isAuthenticated) {
        BGC_SendCallback("BackendGameCenterListener", "OnReportScoreFailed", "NotAuthenticated");
        return;
    }
    GKScore *scoreObj = [[GKScore alloc] initWithLeaderboardIdentifier:leaderboardID];
    scoreObj.value = score;
    [GKScore reportScores:@[scoreObj] withCompletionHandler:^(NSError * _Nullable error) {
        if (error) {
            BGC_SendCallback("BackendGameCenterListener", "OnReportScoreFailed", [error.localizedDescription UTF8String]);
        } else {
            BGC_SendCallback("BackendGameCenterListener", "OnReportScoreSuccess", "");
        }
    }];
}

- (void)showLeaderboard:(nullable NSString*)leaderboardID {
    if (!GKLocalPlayer.localPlayer.isAuthenticated) {
        BGC_SendCallback("BackendGameCenterListener", "OnShowLeaderboardFailed", "NotAuthenticated");
        return;
    }
    GKGameCenterViewController *vc = [[GKGameCenterViewController alloc] initWithLeaderboardID:leaderboardID playerScope:GKLeaderboardPlayerScopeGlobal timeScope:GKLeaderboardTimeScopeAllTime];
    vc.gameCenterDelegate = self;
    vc.viewState = GKGameCenterViewControllerStateLeaderboards;
    UIViewController *root = UIApplication.sharedApplication.keyWindow.rootViewController;
    dispatch_async(dispatch_get_main_queue(), ^{
        [root presentViewController:vc animated:YES completion:nil];
    });
}

- (void)loadTopScores:(NSString*)leaderboardID timeScope:(GKLeaderboardTimeScope)timeScope completion:(void (^)(NSArray<GKScore *> * _Nullable, NSError * _Nullable))completion {
    if (!GKLocalPlayer.localPlayer.isAuthenticated) {
        if (completion) completion(nil, [NSError errorWithDomain:@"GameCenter" code:1 userInfo:@{NSLocalizedDescriptionKey:@"Not authenticated"}]);
        return;
    }
    GKLeaderboard *leaderboard = [[GKLeaderboard alloc] initWithPlayers:nil];
    leaderboard.identifier = leaderboardID;
    leaderboard.timeScope = timeScope;
    leaderboard.playerScope = GKLeaderboardPlayerScopeGlobal;
    leaderboard.range = NSMakeRange(1, 100);
    [leaderboard loadScoresWithCompletionHandler:^(NSArray<GKScore *> * _Nullable scores, NSError * _Nullable error) {
        if (completion) completion(scores, error);
    }];
}

- (void)gameCenterViewControllerDidFinish:(GKGameCenterViewController *)gameCenterViewController {
    [gameCenterViewController dismissViewControllerAnimated:YES completion:^{
        BGC_SendCallback("BackendGameCenterListener", "OnLeaderboardClosed", "");
    }];
}

- (void)getMyScore:(NSString*)leaderboardID timeScope:(GKLeaderboardTimeScope)timeScope {
    if (!GKLocalPlayer.localPlayer.isAuthenticated) {
        BGC_SendCallback("BackendGameCenterListener", "OnGetMyScoreFailed", "NotAuthenticated");
        return;
    }

    GKLeaderboard *leaderboard = [[GKLeaderboard alloc] initWithPlayers:@[GKLocalPlayer.localPlayer]];
    leaderboard.identifier = leaderboardID;
    leaderboard.timeScope = timeScope;
    leaderboard.playerScope = GKLeaderboardPlayerScopeGlobal;
    leaderboard.range = NSMakeRange(1, 1);

    [leaderboard loadScoresWithCompletionHandler:^(NSArray<GKScore *> * _Nullable scores, NSError * _Nullable error) {
        if (error) {
            BGC_SendCallback("BackendGameCenterListener", "OnGetMyScoreFailed", [error.localizedDescription UTF8String]);
        } else if (scores.count > 0) {
            GKScore *myScore = scores.firstObject;
            NSString *msg = [NSString stringWithFormat:@"%lld", myScore.value];
            BGC_SendCallback("BackendGameCenterListener", "OnGetMyScoreSuccess", [msg UTF8String]);
        } else {
            BGC_SendCallback("BackendGameCenterListener", "OnGetMyScoreSuccess", "0");
        }
    }];
}

@end

void BGC_SendCallback(const char* objectName, const char* methodName, const char* message) {
    if (!objectName || !methodName) return;
    UnitySendMessage(objectName, methodName, message ? message : "");
}
