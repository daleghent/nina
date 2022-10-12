/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging N Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/* SIMBAD's entry for Sh2-104 had its coordinates updated */
UPDATE dsodetail SET
    ra = '304.4687500'
    dec = '36.8233333'
    lastmodified = '2022-08-30 00:00:00'
    WHERE id = 'Sh2-104';


PRAGMA user_version = 9;