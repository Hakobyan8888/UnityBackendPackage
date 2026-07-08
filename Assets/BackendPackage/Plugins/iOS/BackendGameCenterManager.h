#import <Foundation/Foundation.h>
#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>

NS_ASSUME_NONNULL_BEGIN

@interface BackendGameCenterManager : NSObject <GKGameCenterControllerDelegate>
+ (instancetype)sharedManager;

- (void)authenticateLocalPlayer;
- (BOOL)isAuthenticated;
- (void)reportScore:(int64_t)score forLeaderboard:(NSString*)leaderboardID;
- (void)showLeaderboard:(nullable NSString*)leaderboardID;
- (void)loadTopScores:(NSString*)leaderboardID timeScope:(GKLeaderboardTimeScope)timeScope
           completion:(void(^)(NSArray<GKScore*>* _Nullable scores, NSError* _Nullable error))completion;
- (void)getMyScore:(NSString*)leaderboardID
         timeScope:(GKLeaderboardTimeScope)timeScope;

@end

void BGC_SendCallback(const char* objectName, const char* methodName, const char* message);

NS_ASSUME_NONNULL_END
