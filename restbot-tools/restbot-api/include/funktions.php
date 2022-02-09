<?php
/*--------------------------------------------------------------------------------
	LICENSE:
		This file is part of the RESTBot Project.

		Copyright (C) 2007-2008 PLEIADES CONSULTING, INC

		This program is free software: you can redistribute it and/or modify
		it under the terms of the GNU Affero General Public License as
		published by the Free Software Foundation, either version 3 of the
		License, or (at your option) any later version.

		This program is distributed in the hope that it will be useful,
		but WITHOUT ANY WARRANTY; without even the implied warranty of
		MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
		GNU Affero General Public License for more details.

		You should have received a copy of the GNU Affero General Public License
		along with this program.  If not, see <http://www.gnu.org/licenses/>.
--------------------------------------------------------------------------------*/
require_once "sql.php";
#require_once 'config.php';

function genPipeError($error)
{
	if ($error == "param") {
		print "PARAMATER_ERROR\n";
	} elseif ($error == "auth") {
		print "AUTHENTICATION\n";
		logMessage(
			"auth",
			1,
			"Authentication issue from " .
				$_SERVER["REMOTE_ADDR"] .
				" on " .
				$_SERVER["REQUEST_URI"]
		);
	} elseif ($error == "db") {
		print "DB\n";
	} elseif ($error == "found") {
		print "NOTFOUND\n";
	} elseif ($error == "lookup") {
		print "LOOKUPERROR\n";
	} else {
		print "UNKNOWN ERROR\n";
	}
	die();
}
function genPipeResponse($resp)
{
	print "OK" . $resp . "\n";
	die();
}

#0 Error 1 Warning 2 Info 3 Debug
function logMessage($type, $level, $message)
{
	global $debug;
	global $use_syslog;
	if ($use_syslog) {
		define_syslog_variables();
		if ($debug) {
		}
		if ($level == 0) {
			$s_level = LOG_ERR;
		} elseif ($level == 1) {
			$s_level = LOG_WARNING;
		} elseif ($level == 2) {
			$s_level = LOG_INFO;
		} elseif ($level == 3) {
			$s_level = LOG_DEBUG;
		} else {
			$s_level = LOG_ERR;
		}
		$s_message = $type . " " . $message;
		openlog("restbot", LOG_PID, LOG_DAEMON);
		syslog($s_level, $s_message);
		closelog();
	}
}

function doRest($method, $arguments, $hostname, $session)
{
	global $debug;
	$url = "http://" . $hostname . "/" . $method . "/" . $session . "/";
	if ($debug) {
		logMessage("rest", 3, "Contacting URL - " . $url);
	}
	$ch = curl_init($url);
	curl_setopt($ch, CURLOPT_POST, true);
	curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
	curl_setopt($ch, CURLOPT_POSTFIELDS, $arguments);
	curl_setopt($ch, CURLOPT_TIMEOUT, 60);
	$response = curl_exec($ch);
	curl_close($ch);
	return $response;
}

function rest($method, $arguments)
{
	global $restbots;
	global $debug;
	$tries = count($restbots) + 3;
	$lastid = 0;
	while ($tries > 0) {
		if ($tries == count($restbots) + 3 - count($restbots)) {
			logMessage("rest", 3, "Checked every bot");
		}
		$restbot = restbotFromDB();
		if ($restbot == null && $restbot["id"] != $lastid) {
			$restbot = newRestBot();
			if ($restbot["hostname"] == null || $restbot["session"] == null) {
				logMessage("rest", 0, "Error creating new restbot");
				$tries--;
				continue;
			}
		} else {
			if ($lastid == $restbot["id"]) {
				logMessage("rest", 3, "already tried this bot...");
				$restbot = newRestBot();
			}
		}
		$lastid = $restbot["id"];
		if (!lockBot($restbot["session"])) {
			logMessage("rest", 1, "Unable to lock bot");
			$tries--;
			continue;
		}
		$response = doRest(
			$method,
			$arguments,
			$restbot["hostname"],
			$restbot["session"]
		);
		if (empty($response)) {
			logMessage("http", 0, "Empty response from restbot");
			restbotRemoveByID($restbot["id"]);
			$tries--;
			continue;
		} else {
			logMessage("rest", 3, "Response received" . $response);
		}
		$xml = new SimpleXMLElement($response);
		if ($xml->error == "invalidsession") {
			logMessage("rest", 1, "Removing invalid session");
			restbotRemoveByID($restbot["id"]);
			$tries--;
			continue;
		} elseif (
			$xml->error == "Offline" ||
			$xml->error == "Reconnecting" ||
			$xml->error == "LoggingIn"
		) {
			logMessage("rest", 3, "Awaiting connection to SL");
			releaseBot($restbot["session"]);
			sleep(5);
			$tries--;
			continue;
		} elseif (isset($xml->error)) {
			logMessage("rest", 0, "Unhandled restbot error - " . $xml->error);
			releaseBot($restbot["session"]);
			$tries--;
			continue;
		}
		releaseBot($restbot["session"]);
		return $response;
	}
	logMessage("rest", 3, "Unable to do rest magic");
}

function restbotConnect($first, $last, $pass, $host, $hostpass)
{
	global $debug;
	$url = "http://" . $host . "/establish_session/" . $hostpass . "/";
	if ($debug) {
		logMessage("newrest", 3, "Attempting to establish new bot at " . $url);
	}
	$ch = curl_init($url);
	$arguments = "first=" . $first . "&last=" . $last . "&pass=" . md5($pass);
	curl_setopt($ch, CURLOPT_POST, true);
	curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
	curl_setopt($ch, CURLOPT_POSTFIELDS, $arguments);
	curl_setopt($ch, CURLOPT_TIMEOUT, 10);
	$response = curl_exec($ch);
	curl_close($ch);
	if (empty($response)) {
		if ($debug) {
			logMessage("newrest", 3, "Empty response from supposed server");
		}
		return null;
	}
	$xml = new SimpleXMLElement($response);
	if (isset($xml->error)) {
		return ["error" => $xml->error];
	} else {
		return [
			"existing" => $xml->existing_session,
			"session" => $xml->session_id,
		];
	}
}

function newRestBot()
{
	global $restbots;
	global $debug;
	$thisbot = $restbots[rand(0, count($restbots) - 1)];
	$bot = restbotConnect(
		$thisbot["first"],
		$thisbot["last"],
		$thisbot["pass"],
		$thisbot["host"],
		$thisbot["key"]
	);
	if (!isset($bot["error"])) {
		$restbot = ["hostname" => $thisbot["host"], "session" => $bot["session"]];
		if ($bot["existing"] == "true") {
			if (restbotIsLocked($restbot["session"])) {
				logMessage("newrest", 2, "Not using locked bot");
				return null;
			}
		}
		if (
			$xml->existing_session != "true" ||
			!sessionAlreadyExists($restbot["session"])
		) {
			restbotRemoveBySession($restbot["session"]);
			$restbot["id"] = restbotAddToDB(
				$restbot["session"],
				$restbot["hostname"]
			);
		}
		return $restbot;
	}
}

function friendlyUUID($key)
{
	return substr($key, 0, 8) .
		"-" .
		substr($key, 8, 4) .
		"-" .
		substr($key, 12, 4) .
		"-" .
		substr($key, 16, 4) .
		"-" .
		substr($key, 20, 12);
}
?>
